# Решение проблем

[English](../en/troubleshooting.md) · **Русский**

Нажмите **Проверить карту** (Validate map), исправьте ошибки по порядку сверху вниз, затем соберите карту снова.

Найдите сообщение или симптом, который вы видите, и примените исправление. Здесь описаны только частые сообщения проверки; все сообщения перечислены в разделе [Что проверяется](validation.md#catalogue). Сообщения окна **Map Validation** и Console приведены на английском, в точности так, как их показывают инструменты; строки MapBuilder и игры приведены по-русски, с английским вариантом в скобках. Текст в угловых скобках обозначает значение из вашей карты, например `<object>` — имя GameObject.

Разделы страницы:

- [Сообщения проверки](#validation-messages)
- [Ошибки сборки в Console](#build-errors)
- [Неактивные элементы управления](#greyed-out)
- [Состояние сборки](#build-state)
- [Проблемы в игре](#game-problems)
- [Проблемы при отправке](#upload-problems)
- [Проблемы при настройке](#setup-problems)
- [Частые вопросы](#faq)

<a name="validation-messages"></a>
## Сообщения проверки

Окно **Map Validation** выводит эти сообщения после того, как вы нажмёте **Проверить карту** или кнопку **Собрать карту** (Build map). Ошибки останавливают сборку, предупреждения и заметки — нет. У большинства сообщений есть кнопка **Select**, которая выделяет объект или ассет, к которому относится сообщение. Записи сгруппированы по категориям в том же порядке, что и в окне. Как читать окно и пользоваться его фильтрами, см. в разделе [Чтение результатов](validation.md#results).

### Budget

<a name="msg-budget-count"></a>
#### `'Minimap': the map has 0, at least 1 is required.`

Уровень: ошибка (блокирует сборку) · Категория: Budget <!-- src: Assets/Editor/Validation/SceneValidator.cs:198-199 -->

**Причина.** В сцене вне `Garbage` нет компонента Minimap. Если их два или больше, сообщение — `'Minimap': the map has <n>, at most 1 is allowed.` Те же два сообщения категории Budget появляются для каждого типа компонентов, количество которых выходит за лимит, например для слишком большого числа Transform или Rigidbody.

**Исправление.**

- Оставьте в сцене ровно один компонент Minimap — на активном объекте вне любого поддерева `Garbage`. Неактивный Minimap проходит проверку, но не экспортируется.

См. также: [Добавление мини-карты](minimap-and-images.md#minimap) · [Поддерживаемые компоненты и лимиты](reference.md#budgets)

### Components

<a name="msg-animator-no-marker"></a>
#### `'<type>' on '<object>' requires a GameMarkerData Animation marker on…`

Сообщение: `'<type>' on '<object>' requires a GameMarkerData Animation marker on the Animator root. This component is skipped; add the marker to export baked vertex animation.`

Уровень: заметка (не блокирует сборку) · Категория: Components <!-- src: Assets/Editor/Validation/SceneValidator.cs:163-165 -->

**Причина.** Animator или SkinnedMeshRenderer находится вне иерархии, которую запекает маркер Animation. Экспорт пропускает компонент: в игре объект не двигается, а меш, который рисует только SkinnedMeshRenderer, вообще не экспортируется.

**Исправление.**

1. В окне **Hierarchy** выберите GameObject с Animator в корне анимируемой иерархии.
1. Добавьте компонент GameMarkerData и выберите в его меню типа **Animation**.

См. также: [Вершинная анимация объектов](markers.md#animation)

<a name="msg-unsupported-component"></a>
#### `'<type>' on '<object>' is not a supported component and will not be…`

Сообщение: `'<type>' on '<object>' is not a supported component and will not be exported. It is skipped automatically; supported components on this object are still exported.`

Уровень: заметка (не блокирует сборку) · Категория: Components <!-- src: Assets/Editor/Validation/SceneValidator.cs:174-176 -->

**Причина.** На объекте есть компонент, который карта не может хранить, например Terrain, TerrainCollider, AudioSource, Camera или система частиц. Экспорт пропускает этот компонент и сохраняет поддерживаемые компоненты того же объекта.

**Исправление.** Делать ничего не нужно, если пропущенный компонент не влияет на езду:

- Terrain замените мешем с MeshCollider. Unity Terrain не экспортируется, поэтому в игре машина проваливается сквозь него.
- Чтобы скрыть заметку для типа компонента, который вы оставляете намеренно, выполните шаги из раздела [Скрытие заметок для типа компонента](validation.md#skip-types).
- Чтобы исключить вспомогательный объект целиком из проверки и экспорта, задайте ему **Tag** `Garbage`.

См. также: [Unity Terrain не экспортируется](scene-setup.md#before-you-start) · [MapSkipComponentConfig](reference.md#skip-components) · [Организация сцены](scene-setup.md#garbage)

### Geometry

<a name="msg-empty-material-slot"></a>
#### `'<object>' has an empty material slot; it will render as magenta in game.`

Уровень: предупреждение (не блокирует сборку) · Категория: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:784-785 -->

**Причина.** В элементе списка **Materials** у MeshRenderer не назначен материал.

**Исправление.**

1. В окне **Map Validation** нажмите **Select** рядом с сообщением.
1. В окне **Inspector** у MeshRenderer назначьте материал каждому элементу списка **Materials**.

См. также: [Настройка материалов](scene-setup.md#materials)

<a name="msg-lod-levels"></a>
#### `'<object>' has <n> LOD levels; at most 8 are supported and the whole…`

Сообщение: `'<object>' has <n> LOD levels; at most 8 are supported and the whole group is skipped on export. Merge or remove levels.`

Уровень: ошибка (блокирует сборку) · Категория: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:813-816 -->

**Причина.** У LODGroup больше 8 уровней. Экспорт пропустил бы всю группу.

**Исправление.**

1. В окне **Map Validation** нажмите **Select** рядом с сообщением.
1. В окне **Inspector** у LODGroup удалите уровни, чтобы их осталось не больше 8.

См. также: [Использование LOD-групп](scene-setup.md#lod)

<a name="msg-optimization-off"></a>
#### `Build optimization is off: static meshes and colliders are exported…`

Сообщение: `Build optimization is off: static meshes and colliders are exported as-is, without sector chunking, collider simplification or LODs. Large maps get more draw calls, heavier physics and longer loading. Enable it in Build > Optimization.`

Уровень: предупреждение (не блокирует сборку) · Категория: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:858-860 -->

**Причина.** Для этой карты снят флажок **Оптимизировать сборку** (Optimize build). Статические меши и коллайдеры экспортируются как есть, без секторов и LOD, поэтому большая карта в игре рисуется и загружается медленнее.

**Исправление.**

1. В окне **MapBuilder** на вкладке **Сборка** (Build) нажмите **Оптимизация** (Optimization).
1. Установите флажок **Оптимизировать сборку**.
1. Нажмите кнопку **Собрать карту**.

В macOS и Linux не устанавливайте флажок **Оптимизировать сборку**: его нативная библиотека поставляется только для Unity Editor под Windows x64.

См. также: [Оптимизация](build.md#optimization)

<a name="msg-sector-budget"></a>
#### `'<object>' (<mesh>): static <kind>, <n> triangles, world bounds…`

Сообщение: `'<object>' (<mesh>): static <kind>, <n> triangles, world bounds <x> x <y> x <z> m. Exceeds sector budget (<size> m / <n> triangles). Review Build > Optimization; size alone does not prove a performance issue.`

Уровень: предупреждение (не блокирует сборку) · Категория: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:874-876 -->

**Причина.** Статический меш для рендера или включённый MeshCollider, который не является триггером и не выпуклый, больше, чем **Размер сектора (м)** (Sector size (m)), или содержит больше треугольников, чем **Треугольников на меш** (Max triangles per mesh), а сборка экспортирует его целиком: снят флажок **Оптимизировать сборку** или флажок разбиения для этого вида мешей. При стандартных настройках это предупреждение не появляется. Какие меши охватывает проверка, см. в разделе [Бюджет сектора](reference.md#sector-budget). <!-- src: Assets/Editor/Validation/SceneValidator.cs:879 -->

**Исправление.** На вкладке **Сборка**, в разделе **Оптимизация**, установите флажок **Оптимизировать сборку** и флажок разбиения для этого вида мешей, затем соберите карту и прокатитесь по ней, чтобы проверить результат. Если флажок должен остаться снятым, разделите меш в своём 3D-редакторе.

См. также: [Оптимизация](build.md#optimization)

### Lighting

<a name="msg-directional-light"></a>
#### `'<object>' is a Directional light. It lights the SDK preview only; the…`

Сообщение: `'<object>' is a Directional light. It lights the SDK preview only; the game uses its own sun, so it is not exported.`

Уровень: заметка (не блокирует сборку) · Категория: Lighting <!-- src: Assets/Editor/Validation/SceneValidator.cs:739-742 -->

**Причина.** В сцене есть включённый источник Directional, обычно солнце для предпросмотра, например `sunlight` из пресета окружения. Он освещает сцену только в редакторе, а солнце и небо в игре свои. Если источников Directional два или больше, включённых или нет, окно также показывает предупреждение `The map has <n> Directional Lights. These affect the SDK preview only; the game supplies its own environment lighting.`

**Исправление.** Для освещения предпросмотра, например пресета окружения из `Assets/Art/Internal/EnvironmentProfiles/`, исправление не нужно. Чтобы отчёт был короче, поместите освещение предпросмотра внутрь объекта с **Tag** `Garbage`: проверка и экспорт пропускают это поддерево, а источники света по-прежнему освещают сцену в редакторе.

См. также: [Настройка освещения](scene-setup.md#lighting)

<a name="msg-light-type"></a>
#### `'<object>' is a <type> light. Wavefront/Binary exports Point and Spot…`

Сообщение: `'<object>' is a <type> light. Wavefront/Binary exports Point and Spot lights only, so this one will not reach the mod.`

Уровень: предупреждение (не блокирует сборку) · Категория: Lighting <!-- src: Assets/Editor/Validation/SceneValidator.cs:746-749 -->

**Причина.** Включённый источник света не относится ни к Point, ни к Spot, ни к Directional, например это источник Area. Экспортируются только источники Point и Spot, поэтому этот источник освещает сцену только в редакторе.

**Исправление.** Чтобы источник экспортировался, смените его **Type** на **Point** или **Spot**. Чтобы оставить его только для предпросмотра в редакторе, поместите его внутрь объекта с **Tag** `Garbage`: проверка и экспорт пропускают это поддерево, а источник по-прежнему освещает сцену в редакторе.

См. также: [Настройка освещения](scene-setup.md#lighting)

### Markers

<a name="msg-unsupported-marker"></a>
#### `'<object>' uses unsupported marker '<head>'. This marker will be skipped…`

Сообщение: `'<object>' uses unsupported marker '<head>'. This marker will be skipped during export. Supported types are SpawnPoint, Road, Animation and Material.`

Уровень: предупреждение (не блокирует сборку) · Категория: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:240-242 -->

**Причина.** GameMarkerData использует тип маркера, который игра больше не читает: `Ambient`, `TimeObjectActivator` или `NetworkObject`. В окне **Inspector** та же проблема показана как `Unsupported marker: <head>. Select SpawnPoint, Road, Animation or Material, or remove the component.` Такие маркеры есть в примере префаба `Assets/ExamplePrefab/Ambient.prefab` и в примере TestMap.

**Исправление.**

1. В окне **Map Validation** нажмите **Select** рядом с сообщением.
1. В окне **Inspector**, в контекстном меню компонента GameMarkerData, выберите **Remove Component**.

Если объект должен нести маркер, вместо этого выберите в меню типа **SpawnPoint**, поверхность **Road**, **Animation** или **Material**.

См. также: [Удаление неподдерживаемых маркеров](markers.md#unsupported)

<a name="msg-animation-no-animator"></a>
#### `Animation marker requires an Animator with animation clips.`

Уровень: ошибка (блокирует сборку) · Категория: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:255-256 -->

**Причина.** У маркера Animation нет Animator: ни в поле **Animator**, ни на его собственном GameObject. Либо у Animator нет контроллера, либо в контроллере нет клипов.

**Исправление.**

1. В окне **Map Validation** нажмите **Select** рядом с сообщением.
1. В окне **Inspector**, в маркере Animation, назначьте в поле **Animator** компонент Animator из корня анимируемой иерархии.
1. Назначьте этому Animator контроллер, в котором есть хотя бы один клип.

См. также: [Вершинная анимация объектов](markers.md#animation)

<a name="msg-road-no-collider"></a>
#### `'<object>' is marked as Road but has no Collider on it or its children…`

Сообщение: `'<object>' is marked as Road but has no Collider on it or its children, so its surface type will never be used. Add a Mesh/Box/Sphere/Capsule Collider.`

Уровень: ошибка (блокирует сборку) · Категория: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:266-268 -->

**Причина.** GameMarkerData с поверхностью **Road** находится на объекте, у которого нет коллайдера — ни на самом объекте, ни на дочернем. Одного меша для рендера мало: машине не по чему ехать.

**Исправление.**

1. В окне **Map Validation** нажмите **Select** рядом с сообщением.
1. В окне **Inspector** нажмите **Add Component** и добавьте на тот же объект MeshCollider, BoxCollider, SphereCollider или CapsuleCollider.

> [!IMPORTANT]
> Держите маркер Road и коллайдер на одном GameObject. Проверка принимает и коллайдер на дочернем объекте, но игра не назначает такому коллайдеру поверхность.

См. также: [Разметка дорожных поверхностей](markers.md#road)

<a name="msg-no-spawn"></a>
#### `The map has no SpawnPoint marker, so there is nowhere for a car…`

Сообщение: `The map has no SpawnPoint marker, so there is nowhere for a car to appear. Add an empty object with GameMarkerData set to SpawnPoint.`

Уровень: ошибка (блокирует сборку) · Категория: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:281-283 -->

**Причина.** Ни у одного компонента GameMarkerData в сцене не выбран тип **SpawnPoint**. Маркеры внутри объекта с тегом `Garbage` не учитываются. Если в сцене вообще нет GameMarkerData, окно также показывает `The map has no GameMarkerData at all - at minimum it needs a spawn point.` и `'GameMarkerData': the map has 0, at least 1 is required.`

**Исправление.**

1. В окне **Project** откройте `Assets/ExamplePrefab/` и перетащите `SpawnPoint.prefab` в сцену.
1. Переместите новый объект туда, где должна появляться машина.
1. Переименуйте объект. Карта мира в игре показывает каждую точку появления под именем её GameObject.

См. также: [Добавление точек появления](markers.md#spawn)

<a name="msg-render-core-revision"></a>
#### `'<object>': render core material '<material>' (<variant>) was exported…`

Сообщение: `'<object>': render core material '<material>' (<variant>) was exported from material for revision <n>, but this SDK has revision <n> of the variant. The game uses the fallback material until you re-run Export from material on this entry.`

Уровень: предупреждение (не блокирует сборку) · Категория: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:442-445 -->

**Причина.** Запись маркера Material хранит значения, экспортированные для более старой ревизии её варианта render core, чем та, что есть в SDK этого проекта. Пока вы не экспортируете запись заново, игра использует вместо варианта render core запасной материал.

**Исправление.**

1. В окне **Map Validation** нажмите **Select** рядом с сообщением.
1. В окне **Inspector**, в маркере Material, найдите запись для названного материала и нажмите **Export from material**.
1. Нажмите кнопку **Собрать карту**.

См. также: [Замена материалов вариантами render core](markers.md#material)

<a name="msg-render-core-no-diffuse"></a>
#### `'<object>': render core material '<material>' (<variant>) has no diffuse…`

Сообщение: `'<object>': render core material '<material>' (<variant>) has no diffuse texture and will render white. Press 'Export from material' or assign the texture.`

Уровень: предупреждение (не блокирует сборку) · Категория: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:375-377 -->

**Причина.** У записи маркера Material нет диффузной текстуры, поэтому игра рисует поверхность белой.

**Исправление.**

1. В окне **Map Validation** нажмите **Select** рядом с сообщением.
1. В окне **Inspector**, в маркере Material, найдите запись для названного материала и нажмите **Export from material**.
   Запись берёт текстуры из исходного материала.

См. также: [Замена материалов вариантами render core](markers.md#material)

### Meta

<a name="msg-scene-name"></a>
#### `The scene name '<scene>' cannot be used for a build. Start with a…`

Сообщение: `The scene name '<scene>' cannot be used for a build. Start with a letter and use only letters, digits and underscores.`

Уровень: ошибка (блокирует сборку) · Категория: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:39-40 -->

**Причина.** Имя файла сцены должно начинаться с буквы и содержать только буквы, цифры и подчёркивания, например `MyTrack_01`. Пробелы и дефисы не допускаются. <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:32-45 -->

**Исправление.**

1. В окне **Project** переименуйте файл сцены.
1. Откройте переименованную сцену и сохраните её. При сохранении сцена из `Assets/MapResources` добавляется в список сцен сборки.
1. В окне **MapBuilder** на вкладке **Сборка** выберите переименованную сцену в списке **Сцена** (Scene).

См. также: [Создание папки и сцены карты](scene-setup.md#scene)

<a name="msg-track-name"></a>
#### `The track name '<name>' contains characters that cannot be published.…`

Сообщение: `The track name '<name>' contains characters that cannot be published. Use letters, digits, spaces and -_'.,:!?()&+/, with no leading or trailing spaces.`

Уровень: ошибка (блокирует сборку) · Категория: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:54-56 -->

**Причина.** **Название** (Name) может содержать только буквы, цифры, пробелы и `- _ ' . , : ! ? ( ) & + /`, без пробела в начале и в конце, и не длиннее 50 символов. Слишком длинное название даёт `The track name is <n> characters; this vendor allows 50.` Пустое — `The track has no name. Fill in Name on the Map tab.` <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:8; Assets/Resources/CarX.Modding/ModIoConfig.asset:28 -->

**Исправление.**

- В окне **MapBuilder** на вкладке **Карта** исправьте **Название**.

См. также: [Заполнение вкладки «Карта»](map-settings.md#map-tab)

<a name="msg-summary-length"></a>
#### `The summary is <n> characters; this vendor allows 250.…`

Сообщение: `The summary is <n> characters; this vendor allows 250. Shorten the Summary field.`

Уровень: ошибка (блокирует сборку) · Категория: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:91-95 -->

**Причина.** Краткое описание длиннее 250 символов. MapBuilder берёт только первую строку поля **Краткое описание (необязательно)** (Summary (optional)); если поле пустое — первую строку поля **Описание** (Description), и тогда сообщение заканчивается на `It is taken from the first line of the description - add a shorter Summary instead.` **Описание** длиннее 50 000 символов даёт похожую ошибку: `The description is <n> characters; this vendor allows 50000.` <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:29-30 -->

**Исправление.**

- В окне **MapBuilder** на вкладке **Карта**, в поле **Краткое описание (необязательно)**, введите одну строку длиной не больше 250 символов.

См. также: [Заполнение вкладки «Карта»](map-settings.md#map-tab)

<a name="msg-no-icon"></a>
#### `No Icon is assigned. It is the image shown for the map in game and…`

Сообщение: `No Icon is assigned. It is the image shown for the map in game and on the mod page, and both vendors need one before an entry can be created.`

Уровень: ошибка (блокирует сборку) · Категория: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:103-105 -->

**Причина.** У карты не задана **Иконка** (Icon). Иконка становится логотипом на mod.io и изображением карты в игре, и без неё mod.io не может создать публикацию. Отсутствие **Превью** (Preview) — только предупреждение: `No Preview is assigned. The map will have no large image on the loading screen.`

**Исправление.**

- В окне **MapBuilder** на вкладке **Карта** назначьте PNG в поле **Иконка · 16:9** (Icon · 16:9).

См. также: [Требования к изображениям](minimap-and-images.md#images)

<a name="msg-icon-read-write"></a>
#### `The icon '<texture>' has Read/Write disabled. Enable it in the texture…`

Сообщение: `The icon '<texture>' has Read/Write disabled. Enable it in the texture import settings.`

Уровень: ошибка (блокирует сборку) · Категория: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:111-112 -->

**Причина.** Текстура иконки импортирована с выключенным **Read/Write**. Для превью действует то же правило и сообщение `The preview '<texture>' has Read/Write disabled. Enable it in the texture import settings.` Так бывает со снимком, который CaptureCamera сохранил вне `Assets`, а вы потом скопировали в проект: он получает стандартные настройки импорта Unity с выключенным **Read/Write**.

**Исправление.**

1. В окне **Map Validation** нажмите **Select** рядом с сообщением.
   Текстура выделяется в окне **Project**.
1. В окне **Inspector** раскройте **Advanced** и установите флажок **Read/Write**.
1. Нажмите **Apply**.

См. также: [Настройка параметров импорта](minimap-and-images.md#import-settings)

<a name="msg-icon-format"></a>
#### `The <image> is a <extension> file, which is uploaded as is and is not…`

Сообщение: `The <image> is a <extension> file, which is uploaded as is and is not an image a vendor accepts. Save it as PNG.`

Уровень: ошибка (блокирует сборку) · Категория: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:165-167 -->

**Причина.** `<image>` — это `icon` или `preview`. Загрузчик отправляет иконку и превью на mod.io без изменений, поэтому оба файла должны быть в формате PNG. Файл `.jpg` или `.jpeg` даёт только предупреждение: `The <image> is a <extension> file. The uploader uploads the source file as is and the documented format is PNG.` Файл больше лимита даёт `The <image> is <size> mb; the limit is <limit> mb.`

**Исправление.**

1. В графическом редакторе сохраните изображение в формате PNG так, чтобы оно отвечало [требованиям к изображениям](minimap-and-images.md#images).
1. В окне **MapBuilder** на вкладке **Карта** назначьте PNG в поле **Иконка · 16:9** или **Превью · 16:9** (Preview · 16:9).

См. также: [Ограничения для изображений](reference.md#limits-images)

<a name="msg-scene-not-validated"></a>
#### `The scene itself was not validated - fix the configuration first…`

Сообщение: `The scene itself was not validated - fix the configuration first, then build again.`

Уровень: заметка (не блокирует сборку) · Категория: Meta <!-- src: Assets/Editor/MapBuilder.cs:292-293 -->

**Причина.** В настройках карты есть хотя бы одна ошибка. Сборка останавливается на настройках и не проверяет сцену, поэтому после их исправления могут появиться новые ошибки.

**Исправление.**

1. Исправьте ошибки категории Meta в окне **Map Validation**.
1. Нажмите **Проверить карту**, чтобы увидеть оставшиеся проблемы сцены.

См. также: [Запуск проверки](validation.md#run)

### Minimap

<a name="msg-bounds-size"></a>
#### `'<object>': Bounds Size is <size>. Set it to the size of the map in…`

Сообщение: `'<object>': Bounds Size is <size>. Set it to the size of the map in world units, or the minimap collapses to a point in game.`

Уровень: ошибка (блокирует сборку) · Категория: Minimap <!-- src: Assets/Editor/Validation/SceneValidator.cs:900-902 -->

**Причина.** Значение X или Y поля **Bounds Size** компонента Minimap равно нулю. Без размера мини-карта в игре сжимается в точку.

**Исправление.**

1. В окне **Map Validation** нажмите **Select** рядом с сообщением.
1. В окне **Inspector**, в поле **Bounds Size**, введите область мира в метрах, которую покрывает изображение мини-карты: **X** — размер мира по X, **Y** — размер мира по Z.

Чтобы мировой масштаб объекта по X и Z автоматически копировался в **Bounds Size**, установите флажок **Lock Size**.

См. также: [Создание мини-карты и обозначение её области](minimap-and-images.md#frame-area)

<a name="msg-no-textures"></a>
#### `'<object>': no textures assigned. Add at least one entry with a Main…`

Сообщение: `'<object>': no textures assigned. Add at least one entry with a Main Texture.`

Уровень: ошибка (блокирует сборку) · Категория: Minimap <!-- src: Assets/Editor/Validation/SceneValidator.cs:908-909 -->

**Причина.** Список **Textures** компонента Minimap пуст. Две связанные ошибки исправляются так же: `'<object>': Textures element <i> has no Main Texture assigned.` и `'<object>': Textures element <i> is a <type>. Only Texture2D can be exported.`

**Исправление.**

1. В окне **Map Validation** нажмите **Select** рядом с сообщением.
1. В окне **Inspector**, в списке **Textures**, добавьте элемент и назначьте в его поле **Main Texture** изображение мини-карты — импортированный PNG. Игра показывает только элемент 0.

См. также: [Назначение изображения мини-карты](minimap-and-images.md#assign-minimap) · [Съёмка изображений с помощью CaptureCamera](minimap-and-images.md#capture)

### Physics

<a name="msg-non-convex-dynamic"></a>
#### `'<object>' has a non-convex MeshCollider driven by a non-kinematic…`

Сообщение: `'<object>' has a non-convex MeshCollider driven by a non-kinematic Rigidbody, which the physics engine no longer supports. Tick Convex, or make the Rigidbody kinematic.`

Уровень: ошибка (блокирует сборку) · Категория: Physics <!-- src: Assets/Editor/Validation/SceneValidator.cs:845-847 -->

**Причина.** MeshCollider со снятым флажком **Convex** находится на объекте Rigidbody или внутри него, а у Rigidbody снят флажок **Is Kinematic**. Физика игры не может двигать невыпуклый меш-коллайдер.

**Исправление.**

1. В окне **Map Validation** нажмите **Select** рядом с сообщением.
1. В окне **Inspector** у MeshCollider установите флажок **Convex**.

Если объект никогда не должен двигаться, вместо этого установите флажок **Is Kinematic** у его Rigidbody.

См. также: [Добавление динамических объектов (Rigidbody)](scene-setup.md#dynamic)

### Scene

<a name="msg-scene-not-opened"></a>
#### `'<scene>' was not opened, so only the configuration was checked.`

Уровень: заметка (не блокирует сборку) · Категория: Scene <!-- src: Assets/Editor/MapBuilder.cs:391 -->

**Причина.** **Проверить карту** нужно было открыть сцену, выбранную в списке **Сцена**, а вы отменили предложение сохранить открытую сцену. Проверены только настройки карты. Заметка также появляется с пустым именем (`'' was not opened, …`), если в списке **Сцена** не выбрана ни одна сцена; см. [Список «Сцена» пуст](#scene-empty). <!-- src: Assets/Editor/MapBuilder.cs:391,399-404 -->

**Исправление.**

- Снова нажмите **Проверить карту** и в запросе сохраните или отмените изменения открытой сцены.

См. также: [Запуск проверки](validation.md#run)

<a name="build-errors"></a>
## Ошибки сборки в Console

Некоторые проблемы обнаруживаются только во время экспорта карты. Они останавливают сборку с исключением в Console, а не в окне **Map Validation**: строка в Console начинается с типа исключения — обычно `InvalidOperationException`, а для материала Layered Lit — `InvalidDataException`. Вкладка **Сборка** затем показывает `Сборка не завершена полностью. Проверьте результаты проверки и Console.` (`Build incomplete. Check validation results and the Console.`). Чтобы открыть Console, выберите **Window** > **General** > **Console**. <!-- src: Assets/Editor/MapBuilder.cs:324-328 -->

<a name="export-errors"></a>
### Ошибки экспорта

В следующей таблице перечислены все ошибки, которые останавливают сборку во время экспорта. Записи после таблицы подробно описывают частые из них.

| Сообщение | Причина | Исправление |
| --- | --- | --- |
| `Rigidbody '<name>' cannot split an enclosing LODGroup. Put the Rigidbody on the LODGroup root.` | Rigidbody стоит на дочернем объекте внутри иерархии LODGroup, а не на объекте с LODGroup. | Перенесите Rigidbody на объект с LODGroup. [Подробнее](#msg-rigidbody-lodgroup) |
| `Rigidbody '<name>': collider '<object>' has a sheared transform. Apply scale in the model first.` | Коллайдер динамического объекта повёрнут внутри родителя с разным масштабом по X, Y и Z. | Задайте каждому родителю коллайдера одинаковый масштаб по X, Y и Z. [Подробнее](#msg-rigidbody-sheared) |
| `Rigidbody '<name>': MeshCollider has no mesh.` | У включённого MeshCollider динамического объекта не назначен **Mesh**. | Назначьте меш в MeshCollider или удалите коллайдер. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:75 --> |
| `Rigidbody '<name>': a dynamic MeshCollider must be Convex.` | У Rigidbody со снятым флажком **Is Kinematic** есть MeshCollider со снятым флажком **Convex**. | Установите флажок **Convex** у MeshCollider. [Подробнее](#msg-rigidbody-convex) |
| `Put Rigidbody on the Animator root or above it; a baked animation cannot contain independent moving bodies.` | Rigidbody находится внутри иерархии Animator, которую запекает маркер Animation. | Перенесите Rigidbody на объект с Animator или на одного из его родителей. [Подробнее](#msg-rigidbody-in-animation) |
| `Animation marker '<name>' needs an Animator inside the exported map.` | Animator маркера — из поля **Animator** или с объекта самого маркера — помечен тегом `Garbage`, находится внутри объекта с тегом `Garbage` или не принадлежит сцене карты. Проверка этого не ищет. | Назначьте Animator из сцены карты вне любого поддерева `Garbage`. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:31-33 --> |
| `Animator '<name>' has multiple Animation markers.` | Два маркера Animation или больше используют один и тот же Animator. | Оставьте по одному маркеру Animation на каждый Animator. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:34-35 --> |
| `Animation marker hierarchies must not overlap.` или `VAT '<name>': nested Animators must be exported as separate animation objects.` | В иерархии анимируемого Animator есть другой Animator — с собственным маркером Animation или без него. | Вынесите внутренний анимируемый объект из иерархии внешнего Animator или удалите внутренний Animator. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:36-38,92-93 --> |
| `Invalid animation speed/phase on '<name>'.` | Значение **Playback speed** отрицательное или не является числом, либо **Initial phase (0..1)** выходит за пределы 0–1. Окно **Inspector** не даёт выйти за эти пределы, значит, значения задал скрипт или ассет, изменённый вручную. | В маркере Animation задайте **Playback speed** не меньше 0, а **Initial phase (0..1)** — от 0 до 1. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:39-41 --> |
| `Animation clip index on '<name>' must be 0..<n>.` | **Initial clip** указывает на клип, которого больше нет в контроллере Animator, например после того как вы удалили из него клипы. | В маркере Animation снова выберите **Initial clip**. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:42-44 --> |
| `VAT sample rate must be 1..60.` | Значение **Bake frames per second** выходит за пределы 1–60. Ползунок в окне **Inspector** не даёт выйти за них, значит, значение задал скрипт или ассет, изменённый вручную. | Задайте **Bake frames per second** от 1 до 60. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:87 --> |
| `Animator '<name>' has no clips.` | У Animator нет контроллера, или в контроллере нет клипов. Проверка сообщает о той же проблеме как `Animation marker requires an Animator with animation clips.` | Назначьте контроллер хотя бы с одним клипом. [Подробнее](#msg-animation-no-animator) <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:89 --> |
| `VAT '<name>': use one mesh detail level per animation; nested LODGroups are not supported.` | В иерархии анимируемого Animator есть LODGroup. | Удалите LODGroup и оставьте по одному мешу на каждую анимируемую часть. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:90-91 --> |
| `Animator '<name>' has no mesh renderers.` | В иерархии анимируемого Animator нет ни одного включённого MeshRenderer или SkinnedMeshRenderer на активном объекте. | Поместите меши, которые должны двигаться, в иерархию Animator. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:109-111 --> |
| `Animation renderer has no mesh.` | У рендерера в анимируемой иерархии нет меша или меш пустой, например у MeshRenderer нет MeshFilter. | Назначьте меш или удалите рендерер. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:112-113 --> |
| `VAT for '<name>' is too large (<width>x<height>). Reduce vertices, clips or sample rate.` | Текстура запечённой вершинной анимации получилась бы слишком большой. | Уменьшите **Bake frames per second**, сократите клипы или число вершин. [Подробнее](#msg-vat-too-large) |
| `Animation mesh material/submesh count differs.` | Число элементов **Materials** у рендерера в анимируемой иерархии не совпадает с числом сабмешей его меша. | Сделайте в списке **Materials** столько элементов, сколько сабмешей у меша. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:139 --> |
| `VAT supports triangle meshes only.` | Меш в анимируемой иерархии состоит из четырёхугольников, линий или точек. | Экспортируйте меш с треугольниками из своего 3D-редактора и импортируйте его снова. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:142 --> |
| `VAT mesh needs vertex normals.` | У меша в анимируемой иерархии нет нормалей. | В настройках импорта модели выберите для **Normals** значение **Import** или **Calculate** и нажмите **Apply**. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:216 --> |
| `VAT vertex is outside supported half-float range.` | Во время анимации вершина анимируемого меша оказывается дальше примерно 31 км от объекта Animator, или её координаты не являются конечными числами. | Держите анимируемые меши рядом с объектом Animator и проверьте масштаб его иерархии. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:221-222 --> |
| `Layered Lit '<material>': '<option>' is not supported by the mod material format.` | Материал Layered Lit использует параметр, который карта не может хранить. `<option>` — свойство шейдера: `_UseHeightBasedBlend` — это **Use Height Based Blend**, `_UseMainLayerInfluence` — **Main Layer Influence**, `_UVBlendMask` — значение **BlendMask UV Mapping**, отличное от **UV0**, а `_UseDensityMode` — режим смешивания по плотности. | В окне **Inspector** материала снимите флажок этого параметра или выберите для **BlendMask UV Mapping** значение **UV0**. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/LayeredPbrWriter.cs:10-25 --> |
| `Layered Lit '<material>': layer <n> requires UV0 mapping for mod export.` | У слоя `<n>` материала значение **Base UV mapping** отличается от **UV0**. Слой 0 — основной. | Выберите для **Base UV mapping** этого слоя значение **UV0**. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/LayeredPbrWriter.cs:65-68 --> |
| `meshoptimizer is unavailable for this Editor platform. Install its native Editor library or disable build optimization.` | Флажок **Оптимизировать сборку** установлен в Unity Editor не под Windows x64. | Снимите флажок **Оптимизировать сборку**. [Подробнее](#msg-meshoptimizer) |
| `Invalid or extreme vertex coordinates in '<mesh>'.` | При установленном флажке **Оптимизировать сборку** у статического меша есть вершина дальше 10 000 км от начала координат мира или вершина, координаты которой не являются конечными числами. `<mesh>` — имя меша, а не объекта. | Найдите объекты, которые используют этот меш, и исправьте их положение или масштаб либо сам меш в своём 3D-редакторе. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneExportOptimization.cs:301-303 --> |
| `A triangle spans too many sectors. Increase the optimization sector size or fix the source mesh.` | При установленном флажке **Оптимизировать сборку** у статического меша есть настолько большой треугольник, что при разрезании на секторы получилось бы больше 100 000 частей. | Разделите большие треугольники в своём 3D-редакторе. Для меша рендера также поможет больший **Размер сектора (м)**. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SectorMeshGeometry.cs:167-169 --> |
| `Shader 'Hidden/ConvertingEx' is not found.` или `VAT normal export shader is missing.` | Не хватает шейдера SDK из `Assets/Plugins/CarX.Modding.Creator`. | Обновите сабмодуль, как описано в разделе [Папка сабмодуля пуста](#submodule-empty). <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/ExportTextureUtility.cs:166-170 --> |
| `Binary map requires both Meta and Binary Map staging data. Rebuild Map and Meta.` или другое сообщение упаковки, например `Missing map texture: <path>` | При экспорте или публикации карты Binary упаковка обнаруживает, что кэш сборки неполный. | Выберите в поле **Что пересобрать** (Rebuild targets) значение `Everything`, нажмите кнопку **Собрать карту**, затем снова экспортируйте или опубликуйте карту. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/BinaryMapPacker.cs:60-62 --> |
| Любое другое исключение во время сборки | Внутренняя ошибка экспортёра. | Снова нажмите кнопку **Собрать карту**. Если ошибка повторяется, создайте issue в [трекере задач](https://github.com/CarXTechnologies/dro-map-uploader/issues) и приложите полный текст из Console. |

<a name="msg-rigidbody-lodgroup"></a>
### `Rigidbody '<name>' cannot split an enclosing LODGroup. Put the…`

Сообщение: `Rigidbody '<name>' cannot split an enclosing LODGroup. Put the Rigidbody on the LODGroup root.`

Уровень: ошибка (прерывает сборку) · Где видно: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:39 -->

**Причина.** Rigidbody стоит на дочернем объекте внутри иерархии LODGroup, а не на объекте с LODGroup, поэтому движущееся тело унесло бы с собой только часть LOD-группы.

**Исправление.**

- Перенесите Rigidbody на объект с компонентом LODGroup.

См. также: [Добавление динамических объектов (Rigidbody)](scene-setup.md#dynamic)

<a name="msg-rigidbody-sheared"></a>
### `Rigidbody '<name>': collider '<object>' has a sheared transform. Apply…`

Сообщение: `Rigidbody '<name>': collider '<object>' has a sheared transform. Apply scale in the model first.`

Уровень: ошибка (прерывает сборку) · Где видно: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:68 -->

**Причина.** У коллайдера динамического объекта скошенная трансформация (shear). Так бывает, когда повёрнутый дочерний объект находится внутри родителя с разным масштабом по X, Y и Z, и игра не может воспроизвести форму такого коллайдера.

**Исправление.**

1. Задайте каждому родителю названного коллайдера одинаковый масштаб по X, Y и Z. Если модели нужен неравномерный масштаб, примените его к мешу в своём 3D-редакторе и импортируйте модель заново.
1. Нажмите кнопку **Собрать карту**.

См. также: [Добавление динамических объектов (Rigidbody)](scene-setup.md#dynamic)

<a name="msg-rigidbody-convex"></a>
### `Rigidbody '<name>': a dynamic MeshCollider must be Convex.`

Уровень: ошибка (прерывает сборку) · Где видно: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:76 -->

**Причина.** Rigidbody со снятым флажком **Is Kinematic** владеет MeshCollider со снятым флажком **Convex**. Обычно проверка сообщает о той же проблеме в категории Physics. MeshCollider без меша на динамическом объекте прерывает сборку с сообщением `Rigidbody '<name>': MeshCollider has no mesh.`

**Исправление.**

1. У MeshCollider установите флажок **Convex**.
1. Убедитесь, что в поле **Mesh** у MeshCollider назначен меш.
1. Нажмите кнопку **Собрать карту**.

См. также: [Добавление динамических объектов (Rigidbody)](scene-setup.md#dynamic)

<a name="msg-rigidbody-in-animation"></a>
### `Put Rigidbody on the Animator root or above it; a baked animation…`

Сообщение: `Put Rigidbody on the Animator root or above it; a baked animation cannot contain independent moving bodies.`

Уровень: ошибка (прерывает сборку) · Где видно: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:46 -->

**Причина.** Rigidbody находится внутри иерархии, которую маркер Animation запекает в вершинную анимацию. Запечённая анимация не может содержать тела, которые двигаются сами по себе.

**Исправление.**

- Перенесите Rigidbody на объект с Animator или на одного из его родителей.

См. также: [Вершинная анимация объектов](markers.md#animation)

<a name="msg-vat-too-large"></a>
### `VAT for '<name>' is too large (<width>x<height>). Reduce vertices…`

Сообщение: `VAT for '<name>' is too large (<width>x<height>). Reduce vertices, clips or sample rate.`

Уровень: ошибка (прерывает сборку) · Где видно: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:128 -->

**Причина.** Текстура, в которой хранится запечённая вершинная анимация, получилась бы слишком большой. Её размер растёт с числом вершин анимируемых мешей, общей длительностью клипов и значением **Bake frames per second**.

**Исправление.**

1. В маркере Animation уменьшите **Bake frames per second** (по умолчанию 30).
1. Если ошибка осталась, сократите или удалите клипы либо уменьшите число вершин анимируемых мешей.
1. Нажмите кнопку **Собрать карту**.

См. также: [Вершинная анимация объектов](markers.md#animation)

<a name="msg-meshoptimizer"></a>
### `meshoptimizer is unavailable for this Editor platform. Install its…`

Сообщение: `meshoptimizer is unavailable for this Editor platform. Install its native Editor library or disable build optimization.`

Уровень: ошибка (прерывает сборку) · Где видно: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneExportOptimization.cs:177 -->

**Причина.** Флажок **Оптимизировать сборку** установлен, а Unity Editor работает на платформе без нативной библиотеки meshoptimizer. Проект поставляет её только для Unity Editor под Windows x64.

**Исправление.**

1. В окне **MapBuilder** на вкладке **Сборка** нажмите **Оптимизация**.
1. Снимите флажок **Оптимизировать сборку**.
1. Нажмите кнопку **Собрать карту**.

После этого проверка показывает предупреждение `Build optimization is off: …` — на этой платформе так и должно быть.

См. также: [Оптимизация](build.md#optimization)

<a name="msg-rigidbody-skipped"></a>
### `Rigidbody '<name>' was skipped: no enabled supported colliders.…`

Сообщение: `Rigidbody '<name>' was skipped: no enabled supported colliders. Geometry and animation will still be exported.`

Уровень: предупреждение (сборка продолжается) · Где видно: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:94 -->

**Причина.** У Rigidbody нет собственного включённого коллайдера Box, Sphere, Capsule или Mesh на активном объекте. Его геометрия всё равно экспортируется, но не двигается.

**Исправление.**

- Добавьте включённый коллайдер на объект Rigidbody или на активный дочерний объект, который не находится внутри другого Rigidbody.

См. также: [Добавление динамических объектов (Rigidbody)](scene-setup.md#dynamic)

<a name="msg-rigidbody-discrete"></a>
### `Rigidbody '<name>': the client uses Unity Physics discrete collision…`

Сообщение: `Rigidbody '<name>': the client uses Unity Physics discrete collision detection; PhysX CCD modes are not exported.`

Уровень: предупреждение (сборка продолжается) · Где видно: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:41 -->

**Причина.** В поле **Collision Detection** у Rigidbody выбран непрерывный режим. Игра всегда использует дискретное обнаружение столкновений, поэтому эта настройка не экспортируется.

**Исправление.**

- У Rigidbody выберите в поле **Collision Detection** значение **Discrete**, чтобы редактор вёл себя так же, как игра.

См. также: [Добавление динамических объектов (Rigidbody)](scene-setup.md#dynamic)

<a name="greyed-out"></a>
## Неактивные элементы управления

Элемент управления в окне **MapBuilder** недоступен, пока не выполнены его условия. Пока идёт сборка или отправка, недоступны также список **ЛОКАЛЬНЫЕ КАРТЫ** (LOCAL MAPS), вкладка **Карта**, настройки сборки и список **Публикации** (Publications); дождитесь окончания операции или нажмите **Отмена** (Cancel) в нижней панели окна.

| Элемент | Недоступен, когда | Что делать |
| --- | --- | --- |
| **Проверить карту** | Выполняется операция, или после входа ещё загружаются логотипы публикаций. | Дождитесь окончания операции или загрузки. |
| Кнопка **Собрать карту** | То же, что для **Проверить карту**, или в поле **Что пересобрать** в разделе **Дополнительные настройки** (Advanced settings) выбрано `Nothing`. | Выберите в поле **Что пересобрать** значение `Everything`. |
| **Сцена** | Поле **Что пересобрать** не включает `Map`. | Добавьте `Map` в **Что пересобрать**. |
| **Открыть папку сборки** (Open build folder) | У карты нет папки кэша сборки. | Соберите карту. |
| Кнопка **Экспортировать в папку** (Export to folder) | Сборка не соответствует настройкам карты или значениям полей **Сцена** и **Формат** (Format), или **Папка** (Folder) — не абсолютный путь. | Соберите карту с выбранными значениями полей **Сцена** и **Формат** и введите полный путь, начинающийся с буквы диска, например `C:/Games/CarX Drift Racing Online 2/Mods/MyTrack`. |
| **Создать публикацию** (Create publication) | Вы не вошли, или сборка не соответствует настройкам карты или значениям полей **Сцена** и **Формат**. | Войдите, затем соберите карту. |
| **Обновить публикацию** (Update publication) | Вы не вошли, в списке **Публикации** не выбрана публикация, связанная с этой картой, или сборка не соответствует. | Войдите, выберите строку публикации, затем снова соберите карту. |
| **Отмена** | Отправка на mod.io уже началась. В подсказке написано `Отправку на площадку уже нельзя прервать` (`Sending to the platform cannot be cancelled once started`). | Дождитесь окончания отправки. |
| **Войти** (Sign in) | Выполняется вход или идёт операция. | Пройдите диалоги входа или дождитесь окончания операции. |
| Список **EN**/**RU**, **Выйти** (Sign out) | Выполняется операция. | Дождитесь окончания операции. |
| **Площадка** (Platform) в заголовке окна | Всегда: mod.io — единственная площадка. | Ничего; поле только показывает площадку. |

Если в списке **ЛОКАЛЬНЫЕ КАРТЫ** не выбрана карта, вкладки **Сборка** и **Публикация** (Publish) остаются пустыми и показывают `Выберите локальную карту или добавьте новую.` (`Select a local map or add a new one.`). <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:624 -->

<a name="build-state"></a>
## Состояние сборки

<a name="msg-rebuild-needed"></a>
### `Нужна пересборка`

Где видно: строка карты в списке **ЛОКАЛЬНЫЕ КАРТЫ**; в английском интерфейсе — `Rebuild needed` <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:198 -->

**Причина.** Карта уже собиралась, но сборка больше не соответствует карте. Так бывает, если:

- после сборки вы изменили поле на вкладке **Карта**;
- вы изменили настройку в разделе **Оптимизация** — это сбрасывает часть Map сборки;
- вы создали публикацию — это сбрасывает часть Meta, потому что метаданные той сборки всё ещё содержат временный ID;
- папка кэша сборки пропала, например потому что Windows очистила свою временную папку или вы открыли проект на другом компьютере;
- следующая сборка только Map или только Meta завершилась неудачей.

Сообщение на вкладке **Сборка** зависит от того, что случилось со сборкой: <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:819-832 -->

- Если обе части собраны, но больше не соответствуют карте — после правки на вкладке **Карта** или когда пропал кэш сборки, — на вкладке показано `Сборка устарела или её файлы недоступны. Соберите выбранную сцену в текущем формате.` (`The build is outdated or its files are missing. Build the selected scene in the current format.`). То же сообщение появляется, если после сборки изменить значение поля **Сцена** или **Формат**, а строка в списке **ЛОКАЛЬНЫЕ КАРТЫ** при этом по-прежнему показывает `Готова к экспорту` (`Ready to export`).
- Если одна часть была сброшена — после изменения в разделе **Оптимизация**, нажатия **Создать публикацию** или неудачной сборки только этой части, — на вкладке показано `Геометрия карты ещё не собрана.` (`Map geometry has not been built yet.`) или `Метаданные карты ещё не собраны.` (`Map metadata has not been built yet.`).

**Исправление.**

- В окне **MapBuilder** на вкладке **Сборка** нажмите кнопку **Собрать карту**.

См. также: [Куда записывается сборка](build.md#cache) · [Создание публикации](publish.md#create)

<a name="msg-build-incomplete"></a>
### `Сборка не завершена полностью. Проверьте результаты проверки и Console.`

Где видно: сводка сборки на вкладке **Сборка**; в английском интерфейсе — `Build incomplete. Check validation results and the Console.` <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:387 -->

**Причина.** После последней сборки собраны не обе части, Map и Meta: её остановила ошибка проверки, исключение во время экспорта или **Отмена**. Перед началом сборка сбрасывает части, которые пересобирает, даже если предыдущие сборки были успешными. Если в неудачной сборке в поле **Что пересобрать** были выбраны и Map, и Meta, как по умолчанию, строка карты в списке **ЛОКАЛЬНЫЕ КАРТЫ** показывает `Не собрана` (`Not built`); если не удалась только одна часть — `Нужна пересборка`. <!-- src: Assets/Editor/MapBuilder.cs:271 -->

**Исправление.**

1. Исправьте ошибки в окне **Map Validation**. Оно открывается само после сборки, которая нашла проблемы.
1. Исправьте ошибки в Console, как описано в разделе [Ошибки сборки в Console](#build-errors).
1. Нажмите кнопку **Собрать карту**.

См. также: [Проверка карты](validation.md)

<a name="game-problems"></a>
## Проблемы в игре

Локальные карты загружаются из папки `Mods` рядом с исполняемым файлом игры, только на ПК и только в сборках игры с поддержкой модов. Как узнать, загружает ли их ваша копия игры, см. в разделе [Проверка загрузки локальных карт в игре](test-in-game.md#check-game). Как экспортировать и загрузить карту, см. в разделе [Тест в игре](test-in-game.md).

<a name="map-missing"></a>
### Карта не появляется в игре

**Причина.** Игра показывает локальную карту, только если находит непустую папку непосредственно в `<game folder>/Mods/`, где `<game folder>` — папка с исполняемым файлом игры. Каждая вложенная папка — это одна карта; папки, имена которых начинаются с `.`, пропускаются. Вкладка **Моды** (Mods) экрана **Выбор трека** (Track Selection) появляется, только если игра нашла хотя бы одну карту. <!-- src: game -->

**Исправление.**

1. Проверьте путь. Файлы карты должны лежать непосредственно в `<game folder>/Mods/<MapName>/`, например `<game folder>/Mods/<MapName>/mod.cxmod` для сборки Binary. Файлы, лежащие на одну папку глубже, не загружаются.
1. Убедитесь, что папка не пуста. Если экспорт не удался, вкладка **Публикация** показывает `Нечего экспортировать — подробности в Console.` (`Nothing to export — see the Console.`).
1. В игре снова откройте экран **Выбор трека**. Игра заново читает папку `Mods` каждый раз, когда показывает список карт.
1. Если карта так и не появилась, чтобы узнать, загружает ли ваша копия игры локальные карты, экспортируйте пример TestMap, как описано в разделе [Проверка загрузки локальных карт в игре](test-in-game.md#check-game).

См. также: [Где игра ищет локальные карты](test-in-game.md#local-maps) · [Проверка загрузки локальных карт в игре](test-in-game.md#check-game)

<a name="map-twice"></a>
### Карта появляется дважды после публикации

**Причина.** У локальной копии в `<game folder>/Mods/<MapName>/` и у копии, установленной с mod.io, разные ID мода: имя папки и `<modIoId>_<modfileId>`. Игра показывает обе. <!-- src: game -->

**Исправление.**

- Удалите папку `<game folder>/Mods/<MapName>/`.

См. также: [Удаление локальной копии после публикации](test-in-game.md#after-publish)

<a name="car-falls-through"></a>
### Машина проваливается сквозь карту

**Причина.** Одно из следующего:

- У поверхности есть меш для рендера, но нет поддерживаемого коллайдера. Экспортируются только MeshCollider с мешем, а также включённые BoxCollider, SphereCollider и CapsuleCollider, которые не являются триггерами. MeshCollider экспортируется, даже если он выключен или является триггером. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:304-307,339 -->
- Поверхность — это Unity Terrain. Terrain и TerrainCollider не экспортируются, даже если маркер Road на террейне проходит проверку.
- У MeshCollider нет меша. Проверка предупреждает: `'<object>' has a MeshCollider with no mesh assigned, so it collides with nothing.`
- Поверхность находится внутри объекта с тегом `Garbage`, который исключает её из экспорта.

**Исправление.**

1. Дайте каждой поверхности, по которой ездят, поддерживаемый коллайдер вне любого поддерева `Garbage`: MeshCollider с назначенным мешем или включённый BoxCollider, SphereCollider либо CapsuleCollider со снятым флажком **Is Trigger**.
1. Замените любой террейн мешем с MeshCollider.
1. Соберите карту и снова экспортируйте её.

См. также: [Добавление коллайдеров](scene-setup.md#colliders) · [Unity Terrain не экспортируется](scene-setup.md#before-you-start)

<a name="road-grip"></a>
### У дорожной поверхности неправильное сцепление

**Причина.** Одно из следующего:

- Маркер Road и коллайдер находятся на разных GameObject. Игра применяет поверхность маркера Road только к коллайдеру на собственном объекте маркера; любой другой коллайдер получает физический материал по умолчанию. Проверка принимает коллайдер на дочернем объекте, поэтому об этом не сообщает.
- У маркера поверхность `default`, `ice_snowy` или `pavement`. В игре нет шаблонов поверхности с такими именами, поэтому коллайдер не получает дорожной поверхности — ни со значениями, ни без них — и ведёт себя как коллайдер без маркера Road. В журнале игры `Player.log` при этом появляется предупреждение, которое начинается с `Road marker references surface template '<name>'`.
- У маркера назначен **Template Config**. Его значения — из именованного шаблона или **Custom** — полностью заменяют поверхность игры, включая **Surface Type** и **Water Rate**. Начальные значения берутся из значений поверхностей по умолчанию в MapUploader, а для некоторых поверхностей они отличаются от поверхности игры, например **Bump Min** у `grass`. Если шаблон маркера удалён из конфига, маркер экспортирует собственные значения **Custom**. <!-- src: Assets/Scripts/GameMarkerData.cs:14-37 -->

**Исправление.**

1. Поместите каждый маркер Road на GameObject, на котором есть коллайдер.
1. Замените поверхности, которых нет в игре: в меню типа маркера выберите **Road** > **asphalt** вместо `default`, **Road** > **road_icy** вместо `ice_snowy` и **Road** > **paving_slab** вместо `pavement`.
1. Чтобы использовать собственную поверхность игры, задайте в поле **Template Config** маркера значение `None`. Чтобы настроить поверхность самостоятельно, оставьте **Template Config** и задайте все поля шаблона или **Custom**, включая **Surface Type** и **Water Rate**.
1. Соберите карту и снова экспортируйте её.

См. также: [Разметка дорожных поверхностей](markers.md#road) · [Когда применяются физические значения](reference.md#surface-values) · [Шаблоны значений дороги](markers.md#templates)

<a name="materials-wrong"></a>
### Материалы пурпурные, белые или без текстур

**Причина.** Одно из следующего:

- В MeshRenderer есть пустой слот материала; игра рисует его пурпурным. Проверка предупреждает об этом.
- У материала render core из маркера Material нет диффузной текстуры; игра рисует его белым. Проверка предупреждает об этом.
- Текстура материала не является Texture2D, например это render texture. Экспорт пропускает её и выводит в Console `[ObjExporter] Material '<material>' property '<property>' holds a <type>, only Texture2D can be exported. Skipped.` <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlTextureMapWriter.cs:391 -->

**Исправление.**

1. Исправьте материал так, как описывает сообщение проверки или строка в Console.
1. Соберите карту и снова экспортируйте её.

См. также: [Настройка материалов](scene-setup.md#materials)

<a name="lights-off"></a>
### Днём источники света выключены

**Причина.** Так и задумано. В игре экспортированные источники света Point и Spot включаются только ночью. Источники Directional и Area никогда не экспортируются; солнце и небо в игре свои. <!-- src: game -->

**Исправление.**

- Чтобы проверить свои источники света, перед заездом выберите на экране **Моды** в игре ночное время, например **00:00**.

См. также: [Настройка освещения](scene-setup.md#lighting)

<a name="spawn-names"></a>
### Точки появления на карте мира показаны номерами или под неправильными именами

**Причина.** Карта мира в игре показывает каждую точку появления как точку быстрого перемещения под именем её GameObject. Точка появления с пустым именем показывается под своим номером. <!-- src: game -->

**Исправление.**

1. Дайте GameObject каждой точки появления уникальное понятное имя. Одинаковые имена дают предупреждение проверки `<n> spawn points are all named '<name>'. Wavefront/Binary exports the object name as the spawn point's identity, so give each one a distinct, meaningful name.`
1. Соберите карту и снова экспортируйте её.

См. также: [Добавление точек появления](markers.md#spawn)

<a name="minimap-wrong"></a>
### Мини-карта смещена, растянута или отсутствует

**Причина.** Одно из следующего:

- **Bounds Center** или **Bounds Size** не соответствует изображению. **Bounds Center** — мировые X и Z центра изображения; его поле **Y** содержит мировую Z. **Bounds Size** — размер по мировым X и Z, который покрывает изображение. Верх изображения направлен в сторону мировой +Z.
- Соотношение сторон изображения отличается от соотношения X : Y в **Bounds Size**. Игра растягивает изображение по прямоугольнику.
- Объект с компонентом Minimap неактивен. Неактивный Minimap проходит проверку, но не экспортируется.
- Нужное изображение находится не в элементе 0 списка **Textures**. Игра показывает только элемент 0.

**Исправление.**

1. Исправьте компонент Minimap или изображение.
1. Соберите карту и снова экспортируйте её.

См. также: [Добавление мини-карты](minimap-and-images.md#minimap)

<a name="msg-load-failed"></a>
### `Не удалось загрузить модификацию трассы. Попробуйте ещё раз.`

**Причина.** Игра нашла папку карты, но не смогла загрузить карту, или загрузка длилась слишком долго. Очень большие карты могут не загрузиться. В английской версии игры сообщение — `Failed to load the track mod. Please try again.` <!-- src: game -->

**Исправление.**

1. Необязательно: чтобы ни один файл карты не был заблокирован, закройте игру.
1. Удалите всё содержимое `<game folder>/Mods/<MapName>/`, затем снова экспортируйте карту в пустую папку.
1. В игре загрузите карту. Если вы закрывали игру, сначала запустите её.

Если загрузка по-прежнему не удаётся, откройте журнал игры `%USERPROFILE%\AppData\LocalLow\CarX Technologies\CarX Drift Racing Online 2\Player.log` и найдите строку `Failed to load mod '<MapName>'`. Следующее за ней исключение называет причину. <!-- src: game -->

См. также: [Загрузка карты в игре](test-in-game.md#in-game)

<a name="join-local-map"></a>
### Другие игроки не могут войти в комнату на вашей локальной карте

**Причина.** Комната на локальной карте открыта только игрокам, у которых в собственной папке `Mods` есть папка карты с тем же именем. Остальные игроки не видят комнату в списке комнат, а при попытке войти по коду видят `Модификация не установлена` (`Mod Not Installed`) и `Для входа в комнату установите модификацию трассы.` (`Install the track mod to join this room.`). Карта, опубликованная на mod.io, скачивается при входе игрока в сборках игры с поддержкой mod.io. <!-- src: game -->

**Исправление.**

- Чтобы протестировать карту с друзьями, передайте им копию экспортированной папки карты. Они кладут её в свою папку `<game folder>/Mods/` под тем же именем.
- Чтобы карта стала доступна всем, опубликуйте её на mod.io.

См. также: [Тест в игре](test-in-game.md) · [Публикация на mod.io](publish.md)

<a name="msg-room-version"></a>
### `В комнате используется другая версия модификации трассы. Обновите…`

Сообщение в английской версии игры: `The room uses a different version of this track mod. Update the mod or choose another room.`

**Причина.** Каждое нажатие **Обновить публикацию** загружает новый файл, а комната на карте с mod.io принимает только игроков, у которых установлена та же версия, что у хоста. <!-- src: game -->

**Исправление.**

- Дождитесь, пока игра обновит карту с mod.io у хоста и у игроков, затем снова создайте комнату или войдите в неё.

См. также: [Обновление публикации](publish.md#update)

<a name="upload-problems"></a>
## Проблемы при отправке

<a name="msg-map-size"></a>
### `'<scene>': the map is <size> mb and this vendor accepts 4096 mb.`

Уровень: ошибка (прерывает отправку) · Где видно: Console <!-- src: Assets/Editor/MapBuilder.Storage.cs:169-170 -->

**Причина.** Когда вы нажимаете **Создать публикацию** или **Обновить публикацию**, MapBuilder перед отправкой измеряет кэш сборки, и часть с картой оказывается больше своего лимита. Если слишком велика часть с метаданными, сообщение — `'<scene>': the meta is <size> mb and this vendor accepts 24 mb.`; в метаданных хранятся иконка, превью и изображения мини-карты. Лимиты и то, что именно они измеряют, см. в разделе [Размер сборки](reference.md#limits-size).

Затем нижняя панель окна показывает `Публикация не завершена. Подробности в Console.` (`Publication did not complete. See the Console.`) или `Обновление не завершено. Подробности в Console.` (`Update did not complete. See the Console.`).

**Исправление.**

1. Для части с картой уменьшите размеры текстур и удалите геометрию, которая карте не нужна. Для части с метаданными используйте иконку, превью и изображения мини-карты меньшего размера.
1. Нажмите кнопку **Собрать карту**, затем опубликуйте карту снова.

См. также: [Публикация на mod.io](publish.md) · [Куда записывается сборка](build.md#cache)

<a name="publications-empty"></a>
### Список публикаций пуст, хотя публикации у вас есть

**Причина.** MapBuilder не смог прочитать ваши публикации с mod.io, и Console показывает `Could not read your mod.io creations: <reason>`. Список при этом сохраняет прежнее содержимое — обычно `В mod.io пока нет публикаций.` (`No publications on mod.io yet.`). Причина часто звучит как `You have used the input_json parameter with semantically incorrect JSON.`, что не описывает настоящую проблему. Известны две причины: <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoPublisher.cs:126 -->

- Ошибка сервера или сеанса, например сеанс со старого адреса mod.io. Тогда в Console непосредственно перед ошибкой выводится `HTTP Code: [502]`.
- В вашем аккаунте есть публикация без файла, например созданная на сайте mod.io. Плагин mod.io не может прочитать публикацию без файла, поэтому не загружается весь список.

**Исправление.**

1. В окне **MapBuilder** на вкладке **Публикация**, в разделе **Публикации**, нажмите **Обновить список** (Refresh list).
1. Если список по-прежнему не загружается, а Console показывает `HTTP Code: [502]` или ошибку входа, нажмите **Выйти** в заголовке окна **MapBuilder**.
1. Войдите снова, как описано в разделе [Вход в mod.io](publish.md#sign-in).
1. Нажмите **Обновить список**.

Чтобы увидеть исходную ошибку, при желании выполните следующее:

1. В окне **Project** выберите `Assets/Resources/CarX.Modding/ModIoConfig.asset`.
1. В окне **Inspector** выберите для **Log Level** значение `Verbose`.
1. В разделе **Публикации** нажмите **Обновить список** и прочитайте ошибку в Console.
1. Верните **Log Level** значение `Warning`.

Если после повторного входа список всё ещё не загружается, а mod.io показывает в вашем аккаунте публикацию без файла, удалите эту публикацию по её ID. Когда ни одна строка не выбрана, **Удалить публикацию** (Delete publication) запрашивает ID, поэтому удаление работает и без списка: скопируйте ID со страницы публикации на mod.io, выполните шаги из раздела [Удаление публикации, которую не показывает список](publish.md#delete-by-id), затем нажмите **Обновить список**.

> [!CAUTION]
> Удаление стирает с mod.io публикацию и все опубликованные в ней файлы. Отменить его нельзя.

См. также: [Публикация на mod.io](publish.md) · [Вход в mod.io](publish.md#sign-in)

<a name="msg-sign-in-failed"></a>
### `mod.io sign in failed: <reason>`

Уровень: ошибка (вход не выполнен) · Где видно: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoAuthProvider.cs:103 -->

**Причина.** mod.io отклонил код, например потому что срок его действия истёк, или не удалось подключиться. Два связанных сообщения означают, что вы сами прервали вход: `The mod.io terms of use were not accepted.` после **Decline** и `Sign in cancelled.` после отмены запроса адреса электронной почты.

**Исправление.**

1. В заголовке окна **MapBuilder** нажмите **Войти**.
1. Если открылся диалог `mod.io terms of use`, нажмите **Accept**.
1. В поле **Email** введите адрес электронной почты своего аккаунта mod.io и нажмите **Continue**.
1. В поле **Code** введите код из самого нового письма от mod.io и нажмите **Continue**.

См. также: [Вход в mod.io](publish.md#sign-in)

<a name="modio-unavailable"></a>
### Заголовок показывает `Недоступно`

**Причина.** mod.io не смог запуститься, обычно из-за отсутствия подключения. В английском интерфейсе заголовок показывает `Unavailable`. Причину называет блок в верхней части вкладки **Публикация**, например `mod.io failed to initialize: <reason>`. Кнопкам **Проверить карту**, **Собрать карту** и **Экспортировать в папку** mod.io не нужен. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoEditorBootstrap.cs:132 -->

**Исправление.**

1. Проверьте подключение к интернету.
1. Закройте окно **MapBuilder** и снова откройте его через **Tools** > **MapBuilder**. Окно подключается к mod.io при каждом открытии.

См. также: [Вход в mod.io](publish.md#sign-in)

<a name="setup-problems"></a>
## Проблемы при настройке

<a name="ssh-publickey"></a>
### `git@github.com: Permission denied (publickey).`

**Причина.** Проект регистрирует свой сабмодуль `Assets/Plugins/CarX.Modding.Creator` по адресу SSH, а на вашем компьютере нет SSH-ключа для GitHub. <!-- src: .gitmodules:3 -->

**Исправление.**

- В терминале, в папке проекта, выполните эту команду. Она загружает сабмодуль по HTTPS только для этого запуска и не меняет вашу конфигурацию git.

  ```bash
  git -c url."https://github.com/".insteadOf=git@github.com: submodule update --init --recursive
  ```

См. также: [Получение проекта](getting-started.md#clone)

<a name="submodule-empty"></a>
### Папка сабмодуля пуста, и проект не компилируется

**Причина.** `Assets/Plugins/CarX.Modding.Creator` — сабмодуль git. Архив **Code** > **Download ZIP** на GitHub его не содержит, как и клон, сделанный без `--recurse-submodules`.

**Исправление.**

- Если вы скачали ZIP, вместо этого клонируйте проект с помощью git, как описано в разделе [Получение проекта](getting-started.md#clone).
- Если клон сделан без сабмодуля, выполните в папке проекта `git submodule update --init --recursive`.

И клонирование, и эта команда загружают сабмодуль по его адресу SSH. Если одна из них сообщает `git@github.com: Permission denied (publickey).`, выполните вместо неё команду HTTPS из раздела [Permission denied (publickey)](#ssh-publickey). <!-- src: .gitmodules:3 -->

См. также: [Получение проекта](getting-started.md#clone) · [Обновление существующего клона](getting-started.md#update-clone)

<a name="unity-version"></a>
### Проект открывается в другой версии Unity

**Причина.** Проект привязан к Unity 6000.3.19f1, ревизия 7689f4515d75. Если открыть его в другой версии, она обновит файлы проекта на месте. <!-- src: ProjectSettings/ProjectVersion.txt:1-2 -->

**Исправление.**

1. Закройте Unity Editor.
1. Установите Unity 6000.3.19f1 через Unity Hub.
1. В Unity Hub откройте проект в версии 6000.3.19f1.

Если другая версия уже открыла проект, она изменила файлы, например `ProjectSettings/ProjectVersion.txt`. Прежде чем снова открыть проект, выполните `git status` в папке проекта и восстановите командой `git restore <path>` каждый изменённый файл, который вы не редактировали сами.

> [!CAUTION]
> `git restore <path>` отменяет все локальные изменения в этом файле. Не выполняйте эту команду для своих файлов карты.

См. также: [Установка Unity 6000.3.19f1](getting-started.md#unity)

<a name="scene-empty"></a>
### Список «Сцена» пуст

**Причина.** Список **Сцена** показывает только сцены, включённые в списке сцен сборки, а в нём их нет. Тогда MapBuilder продолжает проверять и собирать сцену последней сборки или сцену, которая была открыта, когда вы выбрали карту, — и это может оказаться не та сцена, которую вы ожидаете. Если эта сцена ни разу не сохранялась, проверка сообщает `No target scene is selected. Pick one in Scene on the Build tab.` <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:780-786; Assets/Editor/MapBuilderEditorWindow.Workspace.cs:228-229 --> Сцена из `Assets/MapResources` добавляется в список и включается каждый раз, когда загружается редактор или вы сохраняете сцену, а сцена, которую вы выключили в списке, удаляется из него при следующем сохранении. <!-- src: Assets/Editor/MapBuilderProcessor.cs:79 -->

**Исправление.**

1. Переместите сцену в `Assets/MapResources/<MapName>/`.
1. Откройте сцену и сохраните её.
1. В окне **MapBuilder** на вкладке **Сборка** выберите сцену в списке **Сцена**.

Сцену вне `Assets/MapResources` нужно добавить и включить вручную: выберите **File** > **Build Profiles**, затем в окне **Build Profiles** выберите **Scene List** и добавьте сцену.

См. также: [Выбор сцены и формата](build.md#format)

<a name="faq"></a>
## Частые вопросы

### Можно ли использовать Unity Terrain?

Не напрямую. Terrain и TerrainCollider не экспортируются, поэтому в игре машина проваливается сквозь них. Вместо этого смоделируйте землю как меш с MeshCollider. См. [Unity Terrain не экспортируется](scene-setup.md#before-you-start) и [Импорт моделей](scene-setup.md#import).

### Я работаю в macOS или Linux. Можно ли собирать карты?

Оптимизатор сборки поставляется только для Unity Editor под Windows x64. В другой системе снимите для каждой карты флажок **Оптимизировать сборку** в разделе **Оптимизация** на вкладке **Сборка**, иначе сборка остановится с ошибкой. Чтобы протестировать карту, вам всё равно понадобится игра на ПК с Windows. См. [Начало работы](getting-started.md) и [Оптимизация](build.md#optimization).

### Сколько точек появления можно добавить и для чего они нужны?

Добавьте столько, сколько нужно; требуется хотя бы одна. Каждая точка появления становится точкой быстрого перемещения на карте мира в игре и показывается под именем своего GameObject, а машина стартует в первой из них. См. [Добавление точек появления](markers.md#spawn).

### Почему мои источники света выключены?

Экспортированные источники света Point и Spot включаются в игре только ночью, а источники Directional и Area никогда не экспортируются. См. [Настройка освещения](scene-setup.md#lighting).

### Нужен ли аккаунт mod.io, чтобы собрать или протестировать карту?

Нет. **Проверить карту**, кнопка **Собрать карту** и кнопка **Экспортировать в папку** работают без входа; аккаунт mod.io нужен только для публикации. См. [Вход в mod.io](publish.md#sign-in).

### Могут ли друзья войти на мою локальную карту?

Только если у них в собственной папке `Mods` есть копия папки вашей карты под тем же именем. Чтобы карта стала доступна всем, опубликуйте её на mod.io. См. [Тест в игре](test-in-game.md).

### На каких платформах можно скачать мою карту?

Игроки получают карты с mod.io в сборках игры с поддержкой mod.io. Локальные карты из папки `Mods` работают только на ПК. См. [Публикация на mod.io](publish.md).

### Как сделать публикацию публичной?

**Создать публикацию** создаёт скрытую публикацию, а **Обновить публикацию** никогда не меняет её видимость. Измените видимость на странице публикации на mod.io; адрес страницы показывает подсказка к размеру в строке публикации в списке **Публикации**. См. [Открытие публикации для игроков](publish.md#make-public).

### Как удалить локальную копию после публикации?

Удалите `<game folder>/Mods/<MapName>/`, иначе игра покажет карту дважды. См. [Удаление локальной копии после публикации](test-in-game.md#after-publish).

### Как переименовать карту?

Измените **Название** на вкладке **Карта**, снова соберите карту, затем нажмите **Обновить публикацию** с установленным флажком **Обновить название** (Update title). См. [Обновление публикации](publish.md#update).

### Где файл моей собранной карты?

Сборка записывается в кэш сборки, а не в пакет для игры: `mod.cxmod` создаётся только при экспорте или публикации. Кнопка **Открыть папку сборки** на вкладке **Сборка** открывает кэш. См. [Куда записывается сборка](build.md#cache).

### Как перенести проект на другой компьютер?

Скопируйте или закоммитьте весь проект, включая `Assets/Resources/MapManagerConfig.asset`, в котором хранятся связи с публикациями, записи о сборках, тексты «Что изменилось» и флажки обновления. Язык интерфейса, **Текстуры Binary** (Binary textures), сводки сборок и ваш адрес электронной почты mod.io остаются в EditorPrefs старого компьютера, а кэш сборки — в его временной папке. На новом компьютере снова войдите и снова соберите карту. См. [Где хранятся настройки и сборки](reference.md#storage).

## Дальнейшие шаги

← [Публикация на mod.io](publish.md) · [Оглавление](README.md) · [Справочник](reference.md) →
