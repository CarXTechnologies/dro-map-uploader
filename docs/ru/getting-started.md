# Начало работы

[English](../en/getting-started.md) · **Русский**

Получите проект MapUploader, откройте его в нужной версии Unity и освойтесь в окне **MapBuilder**. Это делается один раз на каждом компьютере, до того как вы начнёте первую карту.

## Перед началом работы

- Windows 10 или 11, 64-разрядная. Оптимизатор сборки поставляется только для редактора Windows x64, а флажок **Оптимизировать сборку** (Optimize build) по умолчанию установлен. В другой системе снимите флажок **Оптимизировать сборку** для каждой карты в разделе [Оптимизация](build.md#optimization) (Optimization), иначе сборка остановится с ошибкой. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/BuildOptimizationSettings.cs:14 -->
- [Git](https://git-scm.com/downloads) — чтобы клонировать проект вместе с сабмодулем.
- [Unity Hub](https://unity.com/download) — чтобы установить редактор и открыть проект.
- Аккаунт Unity, под которым выполнен вход в Unity Hub, с активной лицензией, например Unity Personal. Без лицензии редактор не запускается.
- Unity Editor 6000.3.19f1, ревизия `7689f4515d75`, — именно эта версия. См. [Установка Unity 6000.3.19f1](#unity). <!-- src: ProjectSettings/ProjectVersion.txt:1-2 -->
- Копия CarX Drift Racing Online 2 для ПК, которая загружает локальные карты, — чтобы проверить карту локально. Локальные карты игра читает из папки `Mods` рядом со своим исполняемым файлом. Как узнать, загружает ли их ваша копия, описано в разделе [Проверка загрузки локальных карт в игре](test-in-game.md#check-game). <!-- src: DR3PC1:Assets/Modules/CarX.Modding/Runtime/Providers/DefaultCollectionProvider.cs:28 -->
- Аккаунт mod.io — только для публикации. См. [Публикация на mod.io](publish.md).

<a name="clone"></a>
## Получение проекта

В проект входит git-сабмодуль `Assets/Plugins/CarX.Modding.Creator`, поэтому клонируйте проект с помощью git.

Чтобы получить проект:

1. В папке, где вы храните проекты Unity, откройте терминал, например Git Bash или PowerShell.
1. Клонируйте репозиторий вместе с сабмодулем:

   ```bash
   git clone --recurse-submodules -b feature/2.0/3.0 https://github.com/CarXTechnologies/dro-map-uploader
   ```

   Параметр `-b feature/2.0/3.0` переключает клон на ветку `feature/2.0/3.0`, которую описывает эта документация. Без него клон окажется на ветке репозитория по умолчанию — `target/2.0`, которая использует Unity 2023.2.20f1 и старую версию MapBuilder.

Результат: в новой папке `dro-map-uploader` находится проект, а его папка `Assets/Plugins/CarX.Modding.Creator` не пуста.

> [!WARNING]
> Архив **Code** > **Download ZIP** на GitHub не содержит сабмодуль. Папка `Assets/Plugins/CarX.Modding.Creator` остаётся пустой, и проект не компилируется.

<details>
<summary>Сабмодуль не загружается: Permission denied (publickey) или Host key verification failed</summary>

Сабмодуль зарегистрирован в проекте по SSH-адресу `git@github.com:CarXTechnologies/CarX.Modding.Creator.git`. Без SSH-ключа GitHub клонирование загружает основной проект, но останавливается на сабмодуле с ошибкой `Permission denied (publickey)`. Если этот компьютер ещё ни разу не подключался к GitHub по SSH и вы не подтвердили хост `github.com`, когда SSH об этом спросил, клонирование вместо этого останавливается с ошибкой `Host key verification failed.` <!-- src: .gitmodules:3 -->

Чтобы загрузить сабмодуль по HTTPS:

- В терминале, в папке `dro-map-uploader`, выполните команду:

  ```bash
  git -c url."https://github.com/".insteadOf=git@github.com: submodule update --init --recursive
  ```

  Параметр `-c` действует только на эту команду и не меняет вашу конфигурацию git.

Результат: в папке `Assets/Plugins/CarX.Modding.Creator` появились файлы сабмодуля.

Последующим командам git, которые загружают сабмодуль, например `git pull --recurse-submodules`, нужен тот же параметр `-c url."https://github.com/".insteadOf=git@github.com:`.

</details>

<a name="unity"></a>
## Установка Unity 6000.3.19f1

Проект закреплён за Unity 6000.3.19f1. MapUploader никогда не собирает плеер игры, поэтому модули платформ не нужны.

Чтобы установить редактор:

1. Откройте [страницу релиза Unity 6000.3.19f1](https://unity.com/releases/editor/whats-new/6000.3.19f1) и нажмите **Install**.
   Unity Hub открывает установку версии 6000.3.19f1.
1. В Unity Hub завершите установку, не добавляя модули.

Результат: Unity Hub показывает версию 6000.3.19f1 на странице **Installs**.

<details>
<summary>Установка редактора через офлайн-установщик</summary>

Чтобы воспользоваться офлайн-установщиком:

1. Скачайте [установщик Unity 6000.3.19f1 для Windows](https://download.unity3d.com/download_unity/7689f4515d75/Windows64EditorInstaller/UnitySetup64-6000.3.19f1.exe) и запустите его.
1. В Unity Hub на странице **Installs** нажмите **Locate** и выберите файл `Unity.exe` в папке `Editor` новой установки.

Результат: Unity Hub показывает версию 6000.3.19f1 на странице **Installs**.

</details>

> [!CAUTION]
> Если открыть проект в любой другой версии Unity, она обновит его на месте, и отменить такое обновление сложно. Если Unity Hub или редактор предлагает сменить или обновить версию редактора, откажитесь.

## Открытие проекта

Чтобы открыть проект:

1. В Unity Hub на странице **Projects** выберите **Add** > **Add project from disk**.
1. Выберите папку `dro-map-uploader` — ту, в которой лежат папки `Assets`, `Packages` и `ProjectSettings`, — и нажмите **Select Folder**.
   Проект появляется в списке с версией редактора 6000.3.19f1.
1. Выберите проект в списке.
   Unity открывает его. Первый импорт занимает некоторое время: Unity импортирует весь HDRP-проект и создаёт папку `Library`.

Результат: в строке меню Unity есть меню **Tools** с пунктом **MapBuilder**. Если вместо этого Unity сообщает об ошибках компиляции или предлагает перейти в Safe Mode, скорее всего, папка `Assets/Plugins/CarX.Modding.Creator` пуста; загрузите сабмодуль, как описано в разделе [Обновление существующего клона](#update-clone).

> [!NOTE]
> Когда вы работаете в проекте, `git status` показывает изменёнными файлы `ProjectSettings/EditorBuildSettings.asset` и `Assets/MapResources/TestMap/TestMapConfig.asset`, а после сборки `TestMap` — ещё и множество файлов `.meta` в `Assets/MapResources/TestMap/Art/`. MapUploader перезаписывает эти файлы в редакторе, так что это ожидаемо. <!-- src: Assets/Editor/MapBuilderProcessor.cs:79; Assets/Editor/MapMetaConfigIdStamper.cs:64-65; Assets/Plugins/CarX.Modding.Creator/Editor/ExportImporterSettings.cs:33,69 -->

<a name="window"></a>
## Открытие MapBuilder

**MapBuilder** — окно, в котором вы заполняете настройки карты, проверяете и собираете её, экспортируете в папку и публикуете на mod.io.

Чтобы открыть MapBuilder:

- В строке меню Unity выберите **Tools** > **MapBuilder**.

Результат: открывается окно **MapBuilder** с картой, которую вы выбрали последней. В первый раз это `TestMap` — пример карты в `Assets/MapResources/TestMap/`. По умолчанию окно открывается на английском языке; как переключить его на русский, описано в разделе [Выбор языка интерфейса](#language).

На скриншоте — окно с выбранной картой `TestMap` на вкладке **Карта** (Map) до первой сборки.

<img src="../images/ru/window-overview.png" width="1000" alt="Скриншот окна MapBuilder с выбранной картой-примером TestMap; выноски 1–7 на основных областях.">

| # | Элемент | Назначение |
| --- | --- | --- |
| 1 | Состояние аккаунта, кнопка **Войти** (Sign in) / **Выйти** (Sign out) | Показывает состояние mod.io: `Не выполнен вход` (`Not signed in`), `Вход…` (`Signing in…`), `Аккаунт: <имя>` (`Account: <name>`) с кнопкой **Выйти** (Sign out) или `Недоступно` (`Unavailable`), пока mod.io запускается или если запустить его не удалось. Входить нужно только для публикации. См. [Вход в mod.io](publish.md#sign-in). <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:537-543 --> |
| 2 | Поле **Площадка** (Platform) в шапке | Показывает площадку публикации. Это всегда mod.io, изменить её нельзя. |
| 3 | Список выбора языка | Переключает окно между **EN** и **RU**. См. [Выбор языка интерфейса](#language). |
| 4 | **ЛОКАЛЬНЫЕ КАРТЫ** (LOCAL MAPS) | Список всех настроек карт в проекте. Поле поиска фильтрует список по названию, а кнопка **+ Новая карта** (+ New map) создаёт настройки карты. См. [Создание или выбор карты](map-settings.md#new-map). |
| 5 | Строка карты | Показывает иконку карты, её название, состояние сборки (`Не собрана` (`Not built`), `Нужна пересборка` (`Rebuild needed`) или `Готова к экспорту` (`Ready to export`)) и строку mod.io. `mod.io: нет связанной публикации` (`mod.io: not linked`) означает, что с картой ещё не связана ни одна публикация. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:198 --> |
| 6 | Вкладки **Карта** (Map), **Сборка** (Build) и **Публикация** (Publish) | На вкладке **Карта** — настройки карты: название, тексты и изображения. На вкладке **Сборка** карта проверяется и собирается. На вкладке **Публикация** сборка экспортируется в папку или публикуется на mod.io. |
| 7 | Нижняя панель | Показывает строку состояния, например `Локальная карта: TestMap` (`Local map: TestMap`). Пока идёт сборка или загрузка, на ней видны индикатор выполнения и кнопка **Отмена** (Cancel). <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:220 --> |

Окно можно пристыковать, как любое окно Unity. Его минимальный размер — 840 × 580 px. Если ширина окна меньше 1120 px, оно переходит на компактную раскладку: подпись в шапке скрывается, колонки описания и изображений на вкладке **Карта** переносятся, если не помещаются рядом, а две колонки вкладки **Публикация** выстраиваются друг под другом. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:60,64; Assets/Editor/MapBuilderEditorWindow.uss:381,395,436 -->

Чтобы проверить, собрать или экспортировать карту, входить не нужно. Вход требуется только для публикации на mod.io.

<a name="language"></a>
## Выбор языка интерфейса

MapBuilder доступен на английском и русском языках. По умолчанию — английский.

Чтобы сменить язык:

- В шапке окна **MapBuilder**, в списке выбора языка (подсказка `Interface language`, в русском интерфейсе — `Язык интерфейса`), выберите **RU** или **EN**.

Результат: окно перерисовывается на выбранном языке, выбранная карта остаётся прежней. MapBuilder запоминает выбор для вашей учётной записи на этом компьютере в ключе EditorPrefs `CarX.MapBuilder.Language` (по умолчанию `en`). <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:14,58 -->

Пока идёт сборка или загрузка, список выбора языка недоступен.

При выбранном **RU** на английском остаются:

- сам Unity Editor;
- инспекторы компонентов, например GameMarkerData, Minimap и CaptureCamera, и диалог сохранения файла `Save Capture`;
- окно **Map Validation**, а также все сообщения проверки и окна Console;
- строки под заголовком **Состав сцены** (Scene contents) на вкладке **Сборка**; <!-- src: Assets/Scripts/ValidItemData.cs:95-98 -->
- большинство подсказок;
- размер, статус и тексты `Old version!` и `Blank <n>` в строках публикаций на вкладке **Публикация**; <!-- src: Assets/Editor/MapBuilderEditorWindow.Items.cs:199-222 -->
- сообщение `Not signed in to https://mod.io/g/carx-dro2.` на вкладке **Публикация**; <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoAuthProvider.cs:154 -->
- диалог выбора папки `External path`; <!-- src: Assets/Editor/MapBuilderEditorWindow.Publish.cs:113 -->
- диалоги входа в mod.io и удаления.

<a name="update-clone"></a>
## Обновление существующего клона

Обновите клон, чтобы получить новые возможности и исправления MapUploader, а также если вы клонировали его без `--recurse-submodules`.

Если у вас нет SSH-ключа для GitHub, добавьте `-c url."https://github.com/".insteadOf=git@github.com:` после `git` в обеих командах — `git pull` и `git submodule update`, — как в разделе [Получение проекта](#clone).

Чтобы обновить проект:

1. Чтобы Unity не импортировал файлы, пока git их меняет, закройте Unity Editor.
1. В терминале, в папке `dro-map-uploader`, загрузите изменения:

   ```bash
   git pull --recurse-submodules
   ```

1. Переключитесь на ветку, которую описывает эта документация; она названа в разделе [Получение проекта](#clone):

   ```bash
   git switch feature/2.0/3.0
   ```

   Клон, сделанный без параметра `-b`, находится на ветке репозитория по умолчанию, и эта команда переводит его на ветку этой документации. Если клон уже на ней, git сообщает, что он уже на этой ветке.

1. Переключите сабмодуль на коммит, который ожидает проект:

   ```bash
   git submodule update --init --recursive
   ```

   Если сабмодуль ещё ни разу не загружался, эта команда его загрузит.

Результат: `git branch --show-current` выводит ветку из команды `git switch`, а `git submodule status` выводит коммит сабмодуля без префикса `-` или `+`. При следующем открытии проекта Unity импортирует изменения.

Если `git pull` или `git switch` останавливается с сообщением `Your local changes to the following files would be overwritten`, восстановите перечисленные файлы, которые перезаписывает MapUploader, и повторите команду:

```bash
git restore ProjectSettings/EditorBuildSettings.asset Assets/MapResources/TestMap
```

> [!CAUTION]
> Команды из блока *Возврат к чистой копии* отменяют все локальные изменения и удаляют все файлы, которые не отслеживает git: незакоммиченные карты и игнорируемые git папки `Library/` и `UserSettings/`. При следующем открытии весь проект импортируется заново. <!-- src: .gitignore:1-2,43 -->

<details>
<summary>Возврат к чистой копии</summary>

Чтобы сбросить клон:

1. Закройте Unity Editor.
1. Чтобы сохранить свои карты, скопируйте папки карт из `Assets/MapResources/` и все остальные изменённые вами файлы в папку за пределами `dro-map-uploader`.
1. В терминале, в папке `dro-map-uploader`, выполните команды:

   ```bash
   git reset --hard
   git clean -xffd
   git submodule update --init --recursive --force
   ```

   `-x` удаляет также игнорируемые файлы, а второй `-f` удаляет также неотслеживаемые папки, которые сами являются git-репозиториями.

Результат: клон совпадает с последним загруженным коммитом, а сабмодуль — с коммитом, который ожидает проект.

</details>

## Дальнейшие шаги

Создайте папку и сцену для своей карты.

← [README MapUploader](../../README.ru.md) · [Оглавление](README.md) · [Подготовка сцены](scene-setup.md) →
