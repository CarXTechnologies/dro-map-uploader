# Площадки публикации (для интеграторов)

[English](../../en/advanced/publishing-vendors.md) · **Русский**

На этой странице описано, как MapUploader публикует карты через SDK площадок: как устроен слой публикации, как подключены SDK и ассеты конфигов площадок, как перенастроить проект на другую игру mod.io и как добавить площадку. Она нужна тем, кто сопровождает MapUploader или использует его слой публикации в другом проекте.

> [!NOTE]
> Авторам карт ничего на этой странице настраивать не нужно: конфиг mod.io для CarX Drift Racing Online 2 входит в проект, это файл `Assets/Resources/CarX.Modding/ModIoConfig.asset`. Как войти и опубликовать карту, описано в разделе [Публикация на mod.io](../publish.md). <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:15-34 -->

## Площадки в этом проекте

В MapUploader есть реализации двух площадок. В окне **MapBuilder** доступна только mod.io. Реализация Steam Workshop компилируется, но окно её никогда не показывает.

| Площадка | ID площадки | Папка SDK | Define | Ассет конфига | В MapBuilder |
| --- | --- | --- | --- | --- | --- |
| mod.io | `modio` | `Assets/Plugins/Modio` | `CARX_MODDING_MODIO` | `ModIoConfig`, заполнен для CarX Drift Racing Online 2 (ID игры `14124`) | Единственная площадка. Поле **Площадка** (Platform) в шапке всегда показывает mod.io, изменить его нельзя. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:142-146 --> |
| Steam Workshop | `steam` | `Assets/Plugins/Facepunch.Steamworks` | `CARX_MODDING_STEAM` | `SteamWorkshopConfig`; у его единственной записи игры снят флажок **Available** | Не показывается. <!-- src: Assets/Resources/CarX.Modding/SteamWorkshopConfig.asset:15-18 --> |

## Как устроен слой публикации

MapUploader никогда не вызывает SDK площадки напрямую. Он работает с одним контрактом, не зависящим от площадки, — он находится в сабмодуле [CarX.Modding.Creator](https://github.com/CarXTechnologies/CarX.Modding.Creator/tree/1f0b558aeffaa069344fb3e909f3fa5ddc1fff95). Реализация каждой площадки лежит в отдельной сборке с define-ограничением (define constraint), поэтому проект компилирует только те площадки, SDK которых в нём есть, а другой проект может добавить площадку, не меняя контракт.

```
Assets/Plugins/CarX.Modding.Creator/          сабмодуль
├── Publishing/
│   ├── Core/                                 CarX.Modding.Creator.Publishing: только Editor, компилируется всегда
│   │   ├── IModPublisher, IModAuthProvider, IModAuthPrompt   то, что реализует каждая площадка
│   │   ├── ModItem, ModItemKey, ModCreateRequest, ModUploadRequest
│   │   ├── ModVendorLimits, ModPublisherContext, ModUploadProgress
│   │   ├── ModPublisherAttribute, ModPublisherRegistry       поиск скомпилированных площадок
│   │   └── ModVendorConfig<T>, ModIoConfig, SteamWorkshopConfig   ассеты с учётными данными и лимитами
│   ├── ModIo/                                CarX.Modding.Creator.Publishing.ModIo: только с CARX_MODDING_MODIO
│   │   └── ModIoPublisher, ModIoAuthProvider, ModIoEditorBootstrap
│   └── Steam/                                CarX.Modding.Creator.Publishing.Steam: только с CARX_MODDING_STEAM
│       └── SteamWorkshopPublisher, SteamWorkshopAuthProvider
└── Editor/Publishing/                        часть CarX.Modding.Creator.Editor, компилируется всегда
    ├── ModPublisherSession                   с ним работает окно инструмента
    ├── ModPublishingDefines                  согласует define-символы площадок с SDK
    └── EditorModAuthPrompt, ModAuthPromptWindow, ModAuthPreferences   диалоги входа, запомненные email и согласие с условиями

Assets/Plugins/Modio/                         SDK mod.io, хранится в этом репозитории
Assets/Plugins/Facepunch.Steamworks/          SDK Steam, хранится в этом репозитории
Assets/Editor/ModPublisherConfigSetup.cs      создаёт ассеты конфигов со значениями CarX
Assets/Resources/CarX.Modding/                ModIoConfig.asset, SteamWorkshopConfig.asset
```

Части работают так:

- **Контракты.** `Publishing/Core` — сборка только для Editor без define-ограничения. Её пространство имён по-прежнему `Plugins.CarX.Modding.Creator.Runtime.Publishing`. Здесь же лежит базовый класс конфигов `ModVendorConfig<T>`, поэтому удаление SDK площадки никогда не превращает сохранённый ассет конфига в ассет с отсутствующим скриптом (missing script).
- **Площадки.** `ModIoPublisher` и `SteamWorkshopPublisher` реализуют `IModPublisher`, у каждого свой `IModAuthProvider`. Их сборки компилируются, только пока задан их define-символ.
- **Обнаружение.** `ModPublisherRegistry` находит площадки через рефлексию, а не по ссылке, потому что ничто не должно жёстко ссылаться на сборку, которой может не быть. Он просматривает загруженные сборки и ищет неабстрактные типы `IModPublisher` с атрибутом `[ModPublisher]` и открытым конструктором, принимающим один `ModPublisherContext`. Помеченный тип без такого конструктора пропускается с ошибкой в Console. Площадки сортируются по параметру `order` атрибута, затем по отображаемому имени: у Steam Workshop `0`, у mod.io `1`.
- **Сессия.** Окно инструмента работает с `ModPublisherSession`, а не с реестром. Сессия создаёт и инициализирует выбранную площадку, вызывает её обработку при каждом обновлении редактора, проводит вход через модальные диалоги редактора и запрашивает подтверждение перед отправкой удаления. ID выбранной площадки она хранит в ключе EditorPrefs `CarX.Modding.Publisher.Vendor.<хеш пути к папке Assets проекта>`. Если там ничего нет, выбирается первая площадка в реестре. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/Publishing/ModPublisherSession.cs:18,38 -->
- **Хост.** MapBuilder создаёт одну сессию, `MapBuilder.session`, с `ModPublisherContext`, в котором заданы правила тегов и видимость новых записей: тег контента `map_2.0`, устаревший тег `Map` и приватная видимость. Тегом `Map` отмечены карты, опубликованные прежними версиями загрузчика; MapBuilder помечает такие записи `Old version!`. Отправляет ли площадка теги на самом деле, решает сама площадка; см. [Известные различия площадок](#differences). <!-- src: Assets/Editor/MapBuilder.Publishing.cs:20-23 -->

### Define-символы площадок

`ModPublishingDefines` согласует три define-символа скриптов с тем, что загружено в редакторе:

| Define | Когда задаётся | Что включает |
| --- | --- | --- |
| `CARX_MODDING_MODIO` | Загружена сборка, имя которой начинается с `Modio` | Сборку `CarX.Modding.Creator.Publishing.ModIo` |
| `CARX_MODDING_STEAM` | Загружена сборка, имя которой начинается с `Facepunch.Steamworks` | Сборку `CarX.Modding.Creator.Publishing.Steam` |
| `CARX_MODDING_MODIO_UPLOAD_PROGRESS` | Существует тип `Modio.Mods.Builder.ModfileUploadProgress`. Он есть только в пропатченном плагине mod.io; см. [SDK площадок](#sdks). | Реальный прогресс упаковки и загрузки для mod.io. Без него публикатор mod.io всё равно компилируется с непропатченным плагином. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/Publishing/ModPublishingDefines.cs:22-40 --> |

Синхронизация не запускается при загрузке домена. Она выполняется в двух случаях:

- при следующем обновлении редактора после первого обращения к `ModPublisherSession`, например когда вы открываете окно **MapBuilder**;
- когда вы выбираете **ModSystem** > **Sync Publishing Defines**.

Синхронизация записывает define-символы для выбранной группы целевых платформ сборки (build target group), выводит каждое изменение в Console и запускает перекомпиляцию, только если что-то изменилось. Все три define-символа для `Standalone` закоммичены в проекте, поэтому свежий клон компилирует обе площадки ещё до первой синхронизации. <!-- src: ProjectSettings/ProjectSettings.asset:687 -->

Проект, который ничего не публикует, например сама игра, синхронизацию никогда не запускает. Нужные define-символы в нём заданы вручную.

<a name="mapbuilder"></a>
### Что в MapBuilder привязано к mod.io

Контракт поддерживает любое число площадок и игр, но окно **MapBuilder** ограничивает и то и другое:

- Окно показывает и выбирает только площадку `modio`. Перед каждой загрузкой списка публикаций оно переключает сессию на `modio`, если сохранена другая площадка.
- В окне нет выбора игры. Его поле **Игра** (Game) с обложкой из магазина Steam и пометкой `(incomplete)` у записей без учётных данных создаётся, но в окно не добавляется. Поэтому MapBuilder публикует в ту игру, которая выбрана в конфиге.
- В разделе **Куда отправить сборку** (Build destination) вариант **Локальная установка** (Local install) есть только у площадки, которая поддерживает локальную установку. mod.io её не поддерживает, поэтому доступны варианты **Площадка** и **Экспорт в папку** (Export to folder).

Окно привязывают к mod.io эти члены классов:

| Файл | Член класса | Что делает |
| --- | --- | --- |
| `Assets/Editor/MapBuilderEditorWindow.cs` | `GetVendorDisplayNames`, `RefreshVendorBar` | Предлагают в поле **Площадка** только площадку `modio` и держат поле неактивным |
| `Assets/Editor/MapBuilderEditorWindow.Items.cs` | `FetchItems` | Переключает сессию на `modio` перед каждой загрузкой списка |
| `Assets/Editor/MapBuilderEditorWindow.Workspace.cs` | `BuildWorkspace` | Заполняет поле **Площадка** только значением mod.io, делает его неактивным и не добавляет в окно поле **Игра** |
| `Assets/Editor/MapBuilderEditorWindow.Workspace.cs` | `PublishedVersionLabel` | Читает связь карты с записью `modio` для подписи `mod.io: …` в строке карты в списке **ЛОКАЛЬНЫЕ КАРТЫ** (LOCAL MAPS) |
| `Assets/Editor/MapBuilder.Publishing.cs` | `CreateNewCommunityItem` | Сообщает шаги прогресса `Подключение к mod.io…` (`Connecting to mod.io…`) и `Отправка файлов / ожидание mod.io…` (`Sending files / waiting for mod.io…`) |

<a name="sdks"></a>
## SDK площадок

Оба SDK закоммичены в `Assets/Plugins` как обычные файлы. Ничего устанавливать не нужно, и вместе с сабмодулем ничего не скачивается. В сабмодуле находятся контракты и реализации площадок, а SDK, с которыми они компилируются, принадлежат проекту, поэтому проект с одной площадкой несёт только её бинарные файлы.

- **mod.io.** Плагин mod.io для Unity лежит в `Assets/Plugins/Modio`. Это не пакет UPM: в папке нет `package.json`, а mod.io распространяет плагин через Asset Store, как `.unitypackage` на странице GitHub Releases или как архив GitHub, который распаковывают в `Assets/Plugins`. <!-- src: Assets/Plugins/Modio/README.md:75-78 --> Поэтому сослаться на него из `Packages/manifest.json` нельзя. Плагину нужен Newtonsoft.Json, который указан в проекте как `com.unity.nuget.newtonsoft-json` 3.2.2. <!-- src: Packages/manifest.json:5 -->
- **Steam.** [Facepunch.Steamworks](https://wiki.facepunch.com/steamworks/) лежит в `Assets/Plugins/Facepunch.Steamworks`: управляемые сборки для Win64, Win32 и Posix, а также нативные библиотеки Steam в `redistributable_bin`.

Плагин mod.io в этом репозитории содержит локальные изменения. Они перечислены в [`Assets/Plugins/Modio/CARX_PATCHES.md`](../../../Assets/Plugins/Modio/CARX_PATCHES.md) и отмечены в исходном коде комментариями `CARX PATCH`:

- Файл мода (modfile) загружается как `<mod id>.zip`, а не `upload.zip`, чтобы по имени опубликованного файла было видно, к какому моду он относится.
- `ModfileBuilder.SetUploadProgress` сообщает прогресс упаковки и загрузки. От этого патча зависит define-символ `CARX_MODDING_MODIO_UPLOAD_PROGRESS`.
- Загрузка по частям (multipart), которая используется для файлов больше 100 МиБ, возвращает результат своего последнего вызова добавления файла мода. Без патча файл мода, который mod.io отклонил, выглядит как успешно загруженный.

Чтобы обновить плагин mod.io:

1. Замените всё содержимое `Assets/Plugins/Modio`, кроме `CARX_PATCHES.md` и его файла `.meta`, более новым релизом из [modio/modio-unity](https://github.com/modio/modio-unity/releases).
   Нужны только `Modio/` и `Unity/`. `CARX_PATCHES.md` — список локальных изменений CarX, в релизе плагина его нет.
1. Необязательно: если вы не хотите компилировать в проекте примеры сцен и UI времени выполнения, удалите `Unity/Examples` и `Unity/UI`.
1. Повторно примените патчи из `CARX_PATCHES.md`.
   Чтобы увидеть, какие патчи на месте, найдите в папке `CARX PATCH`, например командой `grep -rn "CARX PATCH" Assets/Plugins/Modio`.
1. В `CARX_PATCHES.md` удалите патчи, которые исправлены в самом плагине.
1. Выберите **ModSystem** > **Sync Publishing Defines**.
1. Закоммитьте результат.

Результат: проект компилируется, а `CARX_MODDING_MODIO_UPLOAD_PROGRESS` задан, только если патч прогресса загрузки на месте.

Чтобы обновить Facepunch.Steamworks:

1. Замените содержимое `Assets/Plugins/Facepunch.Steamworks` более новым релизом.
1. Закоммитьте результат.

Результат: проект компилируется, а `CARX_MODDING_STEAM` остаётся заданным.

> [!WARNING]
> Замена `Assets/Plugins/Modio` молча удаляет патчи CarX. Если патча прогресса загрузки нет, а `CARX_MODDING_MODIO_UPLOAD_PROGRESS` всё ещё задан, код публикации перестаёт компилироваться.

Чтобы он снова компилировался, выберите **ModSystem** > **Sync Publishing Defines**. Пока в проекте есть ошибки компиляции, Unity держит загруженными сборки, скомпилированные до обновления, поэтому синхронизация может по-прежнему найти пропатченный тип и ничего не изменить. В этом случае удалите define-символ вручную: в **Edit** > **Project Settings** на странице **Player** в разделе **Other Settings** > **Script Compilation** удалите `CARX_MODDING_MODIO_UPLOAD_PROGRESS` из **Scripting Define Symbols** для `Standalone`.

То же относится к удалению папки SDK площадки: если после этого её сборка не компилируется, удалите её define-символ вручную. Ассет конфига площадки остаётся рабочим, потому что его класс находится в базовой сборке, которая компилируется всегда.

<a name="configs"></a>
## Конфиги площадок

Каждая площадка читает учётные данные и лимиты из ScriptableObject в папке `Resources`. Проект хранит оба ассета в `Assets/Resources/CarX.Modding/`: <!-- src: Assets/Editor/ModPublisherConfigSetup.cs:18 -->

- `ModIoConfig.asset` содержит CarX Drift Racing Online 2 на mod.io с заполненными ID игры, API-ключом и адресом API. Авторы карт используют его как есть.
- `SteamWorkshopConfig.asset` содержит Steam app id игры, флажок **Available** снят.

Как ассеты находятся и создаются:

- Площадка загружает первый ассет своего типа из любой папки `Resources`. Если их несколько, она выводит `Multiple <Type> assets found under Resources; using '<name>'.` Держите по одному ассету каждого типа. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModVendorConfig.cs:36 -->
- После каждой перезагрузки скриптов, включая запуск редактора, `ModPublisherConfigSetup` создаёт ассет в `Assets/Resources/CarX.Modding/` для каждой площадки, у которой его нет ни в одной папке `Resources`. Он заполняет название игры, Steam app id `1826420` и URL профиля mod.io, а ID игры mod.io и API-ключ оставляет пустыми. Существующий ассет он никогда не перезаписывает. Этот класс входит в MapUploader, а не в сабмодуль, поэтому в сабмодуле нет значений, привязанных к конкретной игре.
- Поэтому удаление `ModIoConfig.asset` не убирает площадку: при следующей перезагрузке скриптов создаётся ассет без ID игры и API-ключа, и MapBuilder показывает `Недоступно` (`Unavailable`). Вместо этого восстановите закоммиченный файл из git, например командой `git checkout -- Assets/Resources/CarX.Modding/ModIoConfig.asset`. Чтобы вывести игру из использования, снимите её флажок **Available**.
- **Tools** > **CarX Modding** > **Create publisher configs** выполняет ту же проверку по запросу. Если оба ассета существуют, выводится `All mod publisher configs already exist in 'Assets/Resources/CarX.Modding'.` <!-- src: Assets/Editor/ModPublisherConfigSetup.cs:84 -->
- Ассет можно создать и вручную: **Assets** > **Create** > **CarX** > **Modding** > **mod.io Config** или **Steam Workshop Config**.

Каждый конфиг хранит список игр в **Games**. Список заполняется вручную, потому что ID нельзя определить автоматически, и у каждой площадки он свой: Steam app id и ID игры mod.io — разные вещи, их выдают разные люди, а mod.io к тому же нужен API-ключ для каждой игры. Настройки, общие для всех игр площадки, находятся вне списка.

### Поля ModIoConfig

Подписи — это отображаемые имена полей в Unity. В столбце **По умолчанию** указаны значения, закоммиченные в проекте.

У каждой записи в **Games** есть эти поля:

| Параметр | По умолчанию | Назначение | Когда менять |
| --- | --- | --- | --- |
| **Display Name** | `CarX Drift Racing Online 2` | Название игры в списке выбора игры, который MapBuilder не показывает. | Только для другой игры. |
| **Game Id** | `14124` | ID игры на mod.io. Все вызовы API идут к этой игре. | Только для другой игры. |
| **Api Key** | Заполнен | API-ключ игры. Код считает его открытым ключом для чтения: каждый автор получает отдельный токен записи при входе по email, поэтому ассет можно передавать авторам карт. | Только для другой игры. |
| **Server Url** | `https://g-14124.modapi.io/v1` | Адрес API, который mod.io называет API path. Если поле пустое, публикатор использует `https://g-<Game Id>.modapi.io/v1`. | Только если mod.io указывает для игры другой API path, например в своей тестовой среде. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModIoConfig.cs:186 --> |
| **Profile Url** | `https://mod.io/g/carx-dro2` | Публичная страница игры. MapBuilder строит по ней ссылки на публикации (`<Profile Url>/m/<mod id>`) и показывает её в сообщении `Not signed in to <Profile Url>.` | Только для другой игры. |
| **Available** | Установлен | Снимите флажок, чтобы убрать игру из списка, не удаляя запись и её учётные данные. | Когда в списке несколько игр. |

Эти настройки действуют для всех игр:

| Параметр | По умолчанию | Назначение | Когда менять |
| --- | --- | --- | --- |
| **Default Language** | `en` | Код языка, который отправляется в API для локализованных строк. | Редко. |
| **Apply Content Tags** | Снят | Отправляет тег `map_2.0` при создании и обновлении и показывает в списке только ваши моды с тегом `map_2.0` или `Map`. Если флажок снят, теги не отправляются, а в списке видны все моды, которые вы создали для игры. | Только после того, как теги зарегистрированы в панели администратора игры на mod.io (`Admin -> Tags`). Незарегистрированные теги mod.io отклоняет. |
| **Log Level** | `Warning` | Подробность логов плагина mod.io: `None`, `Error`, `Warning`, `Message` или `Verbose`. | Установите `Verbose`, пока разбираетесь с ответом, который не удаётся разобрать. |
| **Max Payload Size In Mb** | `4096` | Наибольший допустимый размер карты. Измеряется по папке `MapTemp` кэша сборки при создании или обновлении публикации. | Чтобы задать более строгий лимит проекта. |
| **Max Meta Size In Mb** | `24` | Наибольший допустимый размер метаданных. Измеряется по папке `MetaTemp` кэша сборки при создании или обновлении публикации. | Чтобы задать более строгий лимит проекта. |
| **Max Preview Size In Mb** | `8` | Наибольший допустимый размер файла изображения в поле **Иконка · 16:9** (Icon · 16:9). Иконка становится логотипом на mod.io, а логотипы больше 8 МБ mod.io отклоняет. | Чтобы задать более строгий лимит проекта. |
| **Max Title Length** | `50` | Наибольшая допустимая длина поля **Название** (Name). **Проверить карту** (Validate map) сообщает о более длинном названии как об ошибке, а публикатор обрезает его до этой длины. | Чтобы задать более строгий лимит проекта. |
| **Max Summary Length** | `250` | Наибольшая допустимая длина краткого описания. mod.io требует краткое описание и ограничивает его 250 символами. | Чтобы задать более строгий лимит проекта. |
| **Max Description Length** | `50000` | Наибольшая допустимая длина поля **Описание** (Description). | Чтобы задать более строгий лимит проекта. <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:25-30 --> |

Лимиты можно менять, потому что это не только правила площадки, но и политика проекта. **Проверить карту** проверяет **Название**, краткое описание, **Описание** и размер файла иконки по лимитам активной площадки. Размеры карты и метаданных проверяются, только когда вы нажимаете **Создать публикацию** (Create publication) или **Обновить публикацию** (Update publication); см. [Ограничения размера при загрузке](../publish.md#size-limits). Если не удаётся создать ни одну площадку, проверка использует встроенные лимиты: 128 символов для поля **Название**, 8000 для краткого описания и поля **Описание** и 1 МБ для иконки. <!-- src: Assets/Editor/MapBuilder.cs:41-46 -->

Как применяются изменения ассета:

- Перезагрузка скриптов не нужна. Перед каждой операцией публикатор сравнивает **Game Id**, **Api Key**, адрес API, **Default Language** и **Log Level** со значениями, с которыми запущен клиент mod.io, и перезапускает клиент, если они различаются.
- Если адрес API меняется, пока в редакторе остаётся тот же домен скриптов, публикатор также выполняет выход, потому что токен, выданный одним адресом, не работает на другом. Адрес меняется вместе с **Server Url** или с **Game Id**, если **Server Url** пустое. Войдите снова.
- Вне режима Play плагин mod.io не подключает свои сервисы сам. `ModIoEditorBootstrap` подключает их для редактора и передаёт плагину значения из `ModIoConfig`, поэтому MapBuilder работает без режима Play и без собственного ассета настроек плагина. В проекте такого ассета нет; **Tools** > **mod.io** > **Edit Settings** создал бы его в `Assets/Resources/mod.io/`, но MapBuilder его не читает.

`ModIoConfig` не принимает в **Server Url** устаревший общий домен `https://api.mod.io/v1`. Этот домен по-прежнему отвечает и даже позволяет войти, но каждый вызов в рамках игры возвращает `REQUESTED_RESOURCE_NOT_FOUND`, что выглядит как отсутствующий мод. С таким значением шапка показывает `Недоступно`, а вкладка **Публикация** (Publish) просит очистить поле: `Server Url on 'ModIoConfig' points at the deprecated https://api.mod.io/v1 domain. Clear the field to use https://g-<id>.modapi.io/v1.` <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModIoConfig.cs:285-286 -->

### Поля SteamWorkshopConfig

У каждой записи в **Games** есть эти поля:

| Параметр | По умолчанию | Назначение | Когда менять |
| --- | --- | --- | --- |
| **Display Name** | `CarX Drift Racing Online 2` | Название игры в списке выбора игры, который MapBuilder не показывает. | Только для другой игры. |
| **App Id** | `1826420` | Steam app id игры. | Только для другой игры. |
| **Available** | Снят | Убирает игру из списка, не удаляя запись. | Только когда MapBuilder начнёт поддерживать Steam Workshop. |

Эти настройки действуют для всех игр:

| Параметр | По умолчанию | Назначение | Когда менять |
| --- | --- | --- | --- |
| **Local Mods Folder** | `Mods` | Папка внутри установки игры, в которую вариант **Локальная установка** копирует сборку: `<папка установки игры>/<Local Mods Folder>/<item id>`. | Если игра читает моды из другой папки. |
| **Max Payload Size In Mb** | `4096` | Наибольший допустимый размер карты. | Чтобы задать более строгий лимит проекта. |
| **Max Meta Size In Mb** | `24` | Наибольший допустимый размер метаданных. | Чтобы задать более строгий лимит проекта. |
| **Max Preview Size In Mb** | `1` | Наибольший допустимый размер файла изображения в поле **Иконка · 16:9**. Steam отклоняет превью больше 1 МБ. | Чтобы задать более строгий лимит проекта. |
| **Max Title Length** | `128` | Наибольшая допустимая длина названия. | Чтобы задать более строгий лимит проекта. |
| **Max Description Length** | `8000` | Наибольшая допустимая длина описания. В Steam нет отдельного краткого описания, поэтому этот лимит действует и на него. | Чтобы задать более строгий лимит проекта. <!-- src: Assets/Resources/CarX.Modding/SteamWorkshopConfig.asset:21-26 --> |

<a name="another-game"></a>
## Настройка другой игры на mod.io

Делайте это, только если используете MapUploader для другой игры, а не для CarX Drift Racing Online 2.

### Перед началом работы

- Аккаунт mod.io, которому видна игра на странице доступа к API, `https://mod.io/me/access`.

### Перенастройка проекта на игру

Чтобы перенастроить проект на другую игру mod.io:

1. В окне **Project** выберите `Assets/Resources/CarX.Modding/ModIoConfig.asset`.
   **Inspector** показывает его поля.
1. В **Games** добавьте запись для своей игры.
   Unity копирует в неё значения последней записи. Следующие шаги перезаписывают каждое из них.
1. В новой записи в поле **Display Name** введите название игры.
1. В поля **Game Id** и **Api Key** введите значения из строки своей игры на странице доступа к API вашего аккаунта mod.io, `https://mod.io/me/access`. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModIoConfig.cs:271,277 -->
1. Удалите значение в поле **Server Url**, чтобы оно стало пустым.
   Тогда публикатор использует `https://g-<Game Id>.modapi.io/v1`.
1. В поле **Profile Url** введите публичную страницу игры, например `https://mod.io/g/<game>`.
1. У записи своей игры установите флажок **Available**.
1. У всех остальных записей снимите флажок **Available**.
   В MapBuilder нет выбора игры, поэтому доступной должна оставаться ровно одна запись.
1. Если окно **MapBuilder** открыто, закройте его.
1. Выберите **Tools** > **MapBuilder**.
   Окно подключается к mod.io с новыми настройками.

Результат: шапка больше не показывает `Недоступно`. Пока вход не выполнен, вкладка **Публикация** показывает `Not signed in to <Profile Url>.` Если какого-то значения не хватает, шапка показывает `Недоступно`, а блок вверху вкладки **Публикация** называет недостающее значение, например `The selected game on 'ModIoConfig' has no api key. Copy it from https://mod.io/me/access.` <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModIoConfig.cs:277; Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoAuthProvider.cs:154 -->

> [!WARNING]
> Копируйте **Game Id** и **Api Key** из одной строки. Аккаунт с доступом к нескольким играм показывает их все, а неверный **Game Id** отправляет карты в другую игру.

Добавляйте новую запись, а не меняйте запись CarX Drift Racing Online 2. Закоммиченный ассет всё ещё хранит ID игры и API-ключ в скрытых полях — с тех времён, когда в ассете ещё не было списка игр. При первом чтении конфига за сессию редактора `ModIoConfig` переносит их в **Games**. Если в этот момент ни у одной записи нет ID игры `14124`, он вставляет запись с API-ключом CarX Drift Racing Online 2 и выбирает её, и MapBuilder без всякой ошибки публикует в CarX Drift Racing Online 2. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModIoConfig.cs:205-234; Assets/Resources/CarX.Modding/ModIoConfig.asset:31-34 -->

В подсказках полей источник назван account settings > Access > API access. Сообщение в Console, которое появляется после того, как проект создал недостающий конфиг, указывает вместо этого на панель администратора игры: `Admin -> API`. <!-- src: Assets/Editor/ModPublisherConfigSetup.cs:79-80 -->

Пока игра скрыта на mod.io, к ней имеют доступ только участники её команды. Авторы карт вне команды не смогут войти и публиковать, пока игра не станет публичной или их не добавят в команду.

Интеграция не использует OAuth-приложения mod.io и персональные токены доступа. Авторы входят по коду из письма, и каждый получает свой токен записи. Никогда не включайте client secret в проект, который вы передаёте авторам карт.

<a name="add-vendor"></a>
## Добавление площадки

Новой площадке нужен код в сабмодуле, а чтобы она появилась в окне **MapBuilder**, — изменения в MapUploader.

Чтобы добавить площадку:

1. В сабмодуле создайте папку `Publishing/<Vendor>/`.
1. В этой папке создайте определение сборки (assembly definition) только для платформы Editor со своим define-ограничением, например `CARX_MODDING_<VENDOR>`. Добавьте ссылки на `CarX.Modding.Creator`, `CarX.Modding.Creator.Publishing` и, если у SDK есть свои определения сборок, на SDK.
   Рабочий пример — `Publishing/ModIo/CarX.Modding.Creator.Publishing.ModIo.asmdef`.
1. Реализуйте `IModPublisher` и `IModAuthProvider`.
   Следуйте контракту, описанному в комментариях к интерфейсам. `InitializeAsync` выполняется перед каждой операцией, поэтому он должен быть дешёвым; используйте его, чтобы замечать изменения конфига. `CreateItemAsync` не должен оставлять запись, если её содержимое не удалось опубликовать. `DeleteItemAsync` должен работать только по ключу, чтобы запись, которую нельзя получить в списке, всё равно можно было удалить.
1. Пометьте публикатор атрибутом `[ModPublisher("<vendor id>", "<Display Name>", <order>)]`.
   Каждая связь с публикацией хранит ID площадки в `ModItemKey`, поэтому не меняйте ID после того, как к нему привязаны карты.
1. Добавьте публикатору открытый конструктор, принимающий один `ModPublisherContext`.
   Без него реестр пропускает публикатор и выводит ошибку в Console.
1. Необязательно: если площадке нужны учётные данные, добавьте подкласс `ModVendorConfig<T>` в `Publishing/Core`.
   Не кладите его в папку площадки: базовая сборка компилируется всегда, поэтому сохранённый ассет переживёт удаление SDK. Чтобы ассет создавался после перезагрузки скриптов, расширьте `Assets/Editor/ModPublisherConfigSetup.cs` в MapUploader.
1. В `Editor/Publishing/ModPublishingDefines.cs` добавьте свой define-символ и префикс имён сборок SDK в закрытый массив `s_vendorSdks`.
1. Выберите **ModSystem** > **Sync Publishing Defines**.
   Console показывает `[CarX.Modding] Detected the SDK for <define>; enabling that publisher.`, и Unity перекомпилирует проект. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/Publishing/ModPublishingDefines.cs:100 -->
1. В MapUploader измените члены классов, которые привязывают окно к mod.io. См. [Что в MapBuilder привязано к mod.io](#mapbuilder).

Результат: после перекомпиляции `ModPublisherSession.AvailableVendors` содержит новую площадку, а окно **MapBuilder** предлагает её в той мере, в какой это позволяют ваши изменения этих членов классов.

Интерфейсы находятся в сабмодуле: [`IModPublisher.cs`](https://github.com/CarXTechnologies/CarX.Modding.Creator/blob/1f0b558aeffaa069344fb3e909f3fa5ddc1fff95/Publishing/Core/IModPublisher.cs) и [`IModAuthProvider.cs`](https://github.com/CarXTechnologies/CarX.Modding.Creator/blob/1f0b558aeffaa069344fb3e909f3fa5ddc1fff95/Publishing/Core/IModAuthProvider.cs).

<a name="differences"></a>
## Известные различия площадок

Площадки расходятся в местах, перечисленных в таблице ниже. Общий контракт сглаживает большую часть различий через `ModVendorLimits`, `ModItem.StatusLabel` и провайдер авторизации.

| Тема | Steam Workshop | mod.io |
| --- | --- | --- |
| Вход | Сессия запущенного клиента Steam. MapBuilder не показывает кнопку **Войти** (Sign in); выход выполняется в клиенте Steam. | Одноразовый код по email в редакторе: условия использования, затем email, затем код из 5 символов. Плагин кэширует токен на диске. См. [Вход в mod.io](../publish.md#sign-in). |
| Смена игры | Нужен перезапуск редактора: клиент Steam сохраняет app id, с которым он запущен. | Клиент перезапускается с настройками новой игры. |
| Создание записи | Steam принял бы пустой элемент, но публикатор всегда отправляет вместе с ним сборку. | mod.io отклоняет запись без названия, краткого описания и логотипа. Публикатор проверяет их и папку сборки до обращения к mod.io. |
| Название | До 128 символов. | До 50 символов; более длинные названия публикатор обрезает. |
| Краткое описание | Отдельного поля нет. При создании краткое описание отправляется как описание элемента. | Обязательно, до 250 символов. MapBuilder отправляет первую строку поля **Краткое описание (необязательно)** (Summary (optional)), если его нет — первую строку поля **Описание**, а если нет и его — `<Name> track.` <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:47-60 --> |
| Описание | Отправляется при обновлении, если установлен флажок **Обновить описание** (Update description). | Отправляется при создании, если поле **Описание** не пустое, с обрезкой до **Max Description Length**. Отправляется при обновлении, если установлен флажок **Обновить описание**, вместе с кратким описанием. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoPublisher.cs:191-194 --> |
| Изображение | Изображение из поля **Иконка · 16:9** как необязательное превью, до 1 МБ. | Изображение из поля **Иконка · 16:9** как обязательный логотип, до 8 МБ. При обновлении оно отправляется, только если установлен флажок **Обновить превью** (Update preview). |
| Версия | Не поддерживается; публикатор её игнорирует. | Сохраняется у каждого загруженного файла. Если поле **Версия** (Version) пустое, отправляется `1.0.0`. <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:62-70 --> |
| Список изменений | Сохраняется как заметка об изменениях (change note) каждой загрузки. | Сохраняется у каждого загруженного файла. |
| Видимость новых записей | Приватная. | Скрытая. |
| Видимость при обновлении | Никогда не меняется. | Никогда не меняется. |
| Статус в списке публикаций | `Public`, `Friends only`, `Private`, `Banned` или `Awaiting agreement`. | Только `File uploaded` или `No file uploaded`. См. [mod.io не сообщает статус мода](#modio-status). |
| Локальная установка | Да: `<папка установки игры>/<Local Mods Folder>/<item id>`, поэтому в разделе **Куда отправить сборку** появляется вариант **Локальная установка**. | Нет. |
| Теги | Всегда отправляет `map_2.0` и показывает элементы с тегом `map_2.0` или `Map`. | Только с установленным флажком **Apply Content Tags**; теги сначала нужно зарегистрировать на mod.io. |
| Удаление записи | `SteamUGC.DeleteFileAsync`. Удалить элемент может только его владелец. | `DELETE /games/{game-id}/mods/{mod-id}` через `ArchiveMod` плагина. Подтверждение в MapBuilder называет удаление безвозвратным, а собственная документация плагина говорит, что вызов архивирует мод и что безвозвратно удалить мод можно только на сайте mod.io. В любом случае мод пропадает из списка. <!-- src: Assets/Plugins/Modio/Modio/Mods/Builder/ModBuilder.cs:757-761 --> |
| Где найти ID записи | В URL элемента, после `?id=`. | На странице мода на mod.io. Где именно, подсказывает запрос `Item id` кнопки **Удалить публикацию** (Delete publication). |

<a name="modio-status"></a>
### mod.io не сообщает статус мода

Сайт mod.io показывает для каждого мода статус проверки (`Pending` или `Accepted`) и признак видимости. Оба значения приходят в ответе API, но плагин для Unity отбрасывает их при построении объекта `Mod`. Низкоуровневый построитель запросов (`ModioAPIRequest.New`, `ModioAPIRequestOptions.AddQueryParameter`) объявлен как `internal`, поэтому у внешней сборки нет поддерживаемого способа прочитать ни то ни другое.

Поэтому список публикаций показывает только то, что можно установить достоверно: загружен ли файл. Подсказка к тексту статуса ссылается на страницу мода, где виден настоящий статус: `Full status and visibility: <Profile Url>/m/<mod id>`. Если плагин позже откроет эти значения в `Mod`, измените только `ModIoPublisher.DescribeStatus`. <!-- src: Assets/Editor/MapBuilderEditorWindow.Items.cs:247 -->

### `INVALID_JSON` обычно означает другое

`ModioAPIUnityClient` превращает любой ответ, который не удалось разобрать, в `ErrorCode.INVALID_JSON`. Сопровождающее его сообщение `You have used the input_json parameter with semantically incorrect JSON.` описывает ошибку запроса, которая почти никогда не бывает настоящей причиной. В MapBuilder оно обычно появляется в Console как `Could not read your mod.io creations: You have used the input_json parameter with semantically incorrect JSON.` <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoPublisher.cs:125-126; Assets/Plugins/Modio/Modio/Errors/Generated/ErrorCode.cs:499 -->

Прежде чем искать причину, прочитайте строки, которые Console или лог редактора (Editor log) показывает прямо перед ним:

- `HTTP Code: [502] Unexpected error from server instead of JSON: <html>…` означает, что сервер ответил страницей ошибки, а с запросом всё в порядке. Если так разом падают все вызовы, связанные с вашим аккаунтом, подозревайте сессию: токен, выданный другим адресом API, например устаревшим `api.mod.io`, позволяет войти, но не работает в нужных вызовах. Выйдите и войдите снова. <!-- src: Assets/Plugins/Modio/Unity/ModioApiUnityClient.cs:469 -->
- Если прямо перед ним ничего не выведено, ответ не удалось десериализовать. Плагин записывает это исключение в лог только на уровне `Verbose`, поэтому установите в `ModIoConfig` для **Log Level** значение `Verbose` и повторите действие. Один известный случай описан в следующем разделе.

### Мод mod.io без файла ломает весь список

В плагине `ModObject.Modfile` — это структура `ModfileObject`, которая не может принимать null, а API присылает `"modfile": null` для мода, у которого ещё нет файла. Newtonsoft.Json не может записать null в такую структуру, поэтому вся страница результатов не десериализуется. Один мод без файла делает нечитаемыми все моды этого пользователя, включая исправные. Сбой проявляется как `INVALID_JSON`, а настоящее исключение пишется в лог только на уровне `Verbose`.

MapUploader не создаёт таких модов:

- Кнопка **Создать публикацию** неактивна, пока не собраны и карта, и её метаданные, потому что публикатор прикрепляет сборку при создании записи, а не создаёт сначала пустую запись.
- Если mod.io принимает новый мод, но отклоняет его файл, публикатор удаляет новый мод и сообщает `mod.io created mod <id> but refused its content: <reason>. The incomplete mod was deleted.` Если и удаление не удалось, сообщение просит удалить мод на mod.io. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoPublisher.cs:520 -->

Мод без файла всё же может появиться иначе, например если его создали на сайте mod.io. Если список не загружается, удалите такой мод по ID: **Удалить публикацию** запрашивает ID, когда ни одна публикация не выбрана, и этот путь не требует списка. См. [Удаление публикации, которую не показывает список](../publish.md#delete-by-id).

### Список публикаций читается через `SyncUserCreations`

`User.GetUserCreations` читает через `ModCache`, и при создании мода этот кэш ничто не очищает. Пустой результат, полученный до появления вашего первого мода, повторялся бы до конца сессии редактора.

Поэтому `ModIoPublisher` вызывает `User.SyncUserCreations()`, который обращается к API, обновляет `User.ModRepository` и читает список оттуда. Запрос ограничен настроенной игрой и не включает удалённые моды. `User.Sync()` тоже очистил бы кэш, но он вдобавок загружает коллекции, кошелёк и права доступа (entitlements), которые этому инструменту не нужны и которые выводят ошибки всякий раз, когда их адреса API недоступны. <!-- src: Assets/Plugins/Modio/Modio/Users/User.cs:662-668 -->

## Дальнейшие шаги

← [Формат мода: Wavefront и Binary](mod-format.md) · [Оглавление](../README.md)
