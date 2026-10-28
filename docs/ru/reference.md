# Справочник

[English](../en/reference.md) · **Русский**

Здесь в одном месте собраны лимиты компонентов, ограничения, дорожные поверхности, правила экспорта, меню и сохраняемое состояние MapUploader. Как пользоваться каждым из них, объясняют страницы задач, а в [глоссарии](#glossary) каждый термин приведён на английском и русском.

<a name="budgets"></a>
## Поддерживаемые компоненты и лимиты

Проверка подсчитывает компоненты всех объектов сцены, выбранной в списке **Сцена** (Scene) на вкладке **Сборка** (Build), включая неактивные объекты и выключенные компоненты. Она пропускает объекты с тегом `Garbage` вместе с их дочерними объектами, а также скрытые служебные объекты, которые не сохраняются в сцене. Количество вне допустимого диапазона — ошибка категории Budget, а ошибки останавливают сборку. <!-- src: Assets/Editor/Validation/SceneValidator.cs:77-121,185-211 -->

| Компонент | Мин. | Макс. | Примечания |
| --- | --- | --- | --- |
| Transform | 1 | 20 000 | Он есть у каждого GameObject, поэтому это число объектов. <!-- src: Assets/Editor/MapSceneRules.cs:12 --> |
| MeshCollider | 0 | 10 000 | Статическая коллизия или коллизия динамического объекта. См. [Добавление коллайдеров](scene-setup.md#colliders). |
| BoxCollider | 0 | 10 000 | Статическая или динамическая коллизия. |
| SphereCollider | 0 | 1000 | Статическая или динамическая коллизия. |
| CapsuleCollider | 0 | 1000 | Статическая или динамическая коллизия. |
| Rigidbody | 0 | 1000 | [Динамический объект](scene-setup.md#dynamic). |
| MeshRenderer | 0 | 10 000 | Видимая геометрия, вместе со своим MeshFilter. |
| MeshFilter | 0 | 10 000 | Меш MeshRenderer. |
| Light | 0 | 500 | Учитывается каждый источник света, в том числе Directional и выключенные, но экспортируются только Point и Spot. См. [Правила экспорта света](#lights). |
| HDAdditionalLightData | 0 | 500 | HDRP добавляет его к каждому источнику света. |
| LODGroup | 0 | 10 000 | [Группа LOD](scene-setup.md#lod). |
| GameMarkerData | 1 | 10 000 | [Маркер](markers.md#types). На карте нужен хотя бы один, потому что ей нужна точка появления. |
| Minimap | 1 | 1 | [Мини-карта](minimap-and-images.md#minimap): ровно одна. <!-- src: Assets/Editor/MapSceneRules.cs:24 --> |

Компоненты, которых нет в таблице, попадают в заметки, а заметки никогда не останавливают сборку: <!-- src: Assets/Editor/Validation/SceneValidator.cs:123-183 -->

- Volume, Reflection Probe и HDAdditionalReflectionData дают заметку категории Format. Они освещают только предпросмотр в редакторе.
- Animator и SkinnedMeshRenderer допускаются в иерархии Animator, у которого есть [маркер Animation](markers.md#animation). В любом другом месте они дают заметку категории Components и не экспортируются.
- Любой другой компонент даёт заметку категории Components и не экспортируется, например Camera, CaptureCamera, AudioSource, системы частиц, Terrain и TerrainCollider. Типы из списка [MapSkipComponentConfig](#skip-components) заметок не дают.
- Отсутствующий скрипт даёт заметку категории Components, а слот его компонента пропускается.

Другие ограничения на содержимое сцены:

| Правило | Ограничение | Что будет при нарушении |
| --- | --- | --- |
| Уровней в одной группе LOD | 8 | Ошибка категории Geometry при проверке, которая останавливает сборку. <!-- src: Assets/Editor/Validation/SceneValidator.cs:811-819 --> |
| **Bake frames per second** маркера Animation | 1–60, по умолчанию 30 | Ползунок в окне **Inspector** не выходит за пределы диапазона. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/Meta/AnimationMeta.cs:12 --> |
| Маркеров Animation на один Animator | 1 | Сборка останавливается с сообщением `Animator '<name>' has multiple Animation markers.` в Console. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:34-35 --> |
| Animator с маркером внутри другого такого Animator | Не допускается | Сборка останавливается с сообщением `Animation marker hierarchies must not overlap.` в Console. |
| Rigidbody в иерархии Animator с маркером | Только на корне Animator или выше него | Сборка останавливается с сообщением `Put Rigidbody on the Animator root or above it; a baked animation cannot contain independent moving bodies.` в Console. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:45-46 --> |

После сборки, в которую входит цель **Map**, панель **Состав сцены** (Scene contents) на вкладке **Сборка** перечисляет в таком виде количество компонентов каждого типа, который есть в сцене. Типы с количеством 0 не выводятся. См. [Сборка карты](build.md). <!-- src: Assets/Scripts/ValidItemData.cs:29-35,95-98 -->

```text
<Type> : <n> count (<min> min - <max> max)
```

<a name="sector-budget"></a>
### Бюджет сектора

Кроме того, проверка выдаёт предупреждение категории Geometry для каждого большого статического меша, который оптимизация сборки не разрежет на секторы. Эти предупреждения никогда не останавливают сборку. <!-- src: Assets/Editor/Validation/SceneValidator.cs:853-883 -->

- **Что проверяется.** Каждый включённый MeshRenderer с MeshFilter и каждый включённый MeshCollider, который не является триггером и не выпуклый, на объекте, который обрабатывает оптимизация: на активном объекте без тега `Garbage`, у которого ни на нём самом, ни на родителе нет Rigidbody, Animator, Animation, LODGroup и маркера, отличного от Road и Material. <!-- src: Assets/Editor/Validation/SceneValidator.cs:879-881 -->
- **Бюджет.** Меш превышает его, если по любому измерению в мировых координатах он больше значения **Размер сектора (м)** (Sector size (m)), по умолчанию `64` м, или содержит больше треугольников, чем **Треугольников на меш** (Max triangles per mesh), по умолчанию `8192`. Коллайдеры сравниваются с этими же двумя значениями, а не с сеткой коллайдеров.
- **Когда появляется предупреждение.** Только для мешей, которые оптимизация не разрежет на секторы: для всех таких мешей, пока снят флажок **Оптимизировать сборку** (Optimize build), для MeshCollider, пока снят флажок **Разбивать статические коллайдеры** (Partition static colliders), и для мешей отрисовки, пока снят флажок **Разбивать статические меши** (Partition static render meshes). При стандартных настройках предупреждений о бюджете сектора нет, и пример TestMap их не получает.
- **Что не попадает в список.** Рендерер или MeshCollider, который оптимизация оставляет целым по другой причине, например прозрачный рендерер или объект с несколькими MeshCollider, сохраняет свой размер и предупреждения не получает. См. [Оптимизация](build.md#optimization).

<a name="limits"></a>
## Ограничения

Кнопки **Проверить карту** (Validate map) и **Собрать карту** (Build map) сверяют с этими ограничениями [настройки карты](#limits-settings) и [изображения](#limits-images), и ошибка останавливает сборку. [Размер сборки](#limits-size) проверяется только при публикации. Большая часть ограничений взята из [настроек mod.io, которые поставляются с проектом](#limits-modio).

<a name="limits-settings"></a>
### Настройки карты

| Поле | Ограничение | Примечания |
| --- | --- | --- |
| Имя сцены | Начинается с буквы; только буквы, цифры и `_` | Имя файла сцены, выбранной в списке **Сцена**, без `.unity`. <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:32-45 --> |
| **Название** (Name) | Обязательно; не больше 50 символов | Буквы любого алфавита, цифры, пробелы и `- _ ' . , : ! ? ( ) & + /`, без пробела в начале и в конце. Показывается в игре и на mod.io. <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:8-30 --> |
| **Версия** (Version) | Нет | Если поле пустое, используется `1.0.0`. Это значение становится версией каждого файла, загруженного на mod.io, и хранится в метаданных карты. <!-- src: Assets/Editor/MapBuilder.cs:176 --> |
| **Краткое описание (необязательно)** (Summary (optional)) | Не больше 250 символов | Используется только первая строка, и ограничение относится к ней. Если поле **Краткое описание (необязательно)** пустое, используется первая строка поля **Описание**; если пусты оба, используется `<Name> track.` <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:47-60 --> |
| **Описание** (Description) | Не больше 50 000 символов | Хранится в метаданных карты. Отправляется на mod.io кнопкой **Создать публикацию** (Create publication), если оно не пустое, и кнопкой **Обновить публикацию** (Update publication) при включённом флажке **Обновить описание** (Update description). См. [Создание публикации](publish.md#create) и [Обновление публикации](publish.md#update). |
| **Авторы** (Authors) | Нет | Игра показывает до 5 имён через `, `, а затем `...`. Если авторов нет, она показывает имя аккаунта mod.io, который создал публикацию. <!-- src: DR3PC1:Assets/DR3/Scripts/States/Controllers/Mods/ModAuthorResolver.cs:10-31 --> |
| **Ссылка** (URL) | Нет | Хранится в метаданных карты. |

Что вводить в каждое поле, см. в разделе [Заполнение вкладки «Карта»](map-settings.md#map-tab).

<a name="limits-images"></a>
### Изображения

| Изображение | Обязательно | Формат | Read/Write | Размер файла | Размер в пикселях |
| --- | --- | --- | --- | --- | --- |
| **Иконка · 16:9** (Icon · 16:9) | Да: если её нет — ошибка | PNG. JPG даёт предупреждение, любой другой формат — ошибку. | Должен быть включён | Не больше 8 МБ | mod.io требует не меньше 512 × 288 и рекомендует 16:9. MapUploader это не проверяет. <!-- src: Assets/Plugins/Modio/Modio/API/Generated/Definitions/RequestObjects/EditModRequest.cs:31 --> |
| **Превью · 16:9** (Preview · 16:9) | Нет: если его нет — предупреждение | PNG. JPG даёт предупреждение, любой другой формат — ошибку. | Должен быть включён | Не больше 10 МБ | Не проверяется; рекомендуется 16:9. <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:14 --> |
| Текстура мини-карты | Да: хотя бы один элемент **Textures**, и у каждого задана **Main Texture** | Texture2D; любой другой тип текстуры — ошибка | Не нужен | Входит в размер метаданных | Не проверяется. Игра использует только **Element 0**. |

Иконка служит и логотипом на mod.io, и изображением карты в игре. Превью не загружается как логотип; оно передаётся внутри метаданных карты. См. [Требования к изображениям](minimap-and-images.md#images).

<a name="limits-size"></a>
### Размер сборки

| Часть | Ограничение | Где измеряется |
| --- | --- | --- |
| Карта: геометрия, коллизия, маркеры, свет, динамические объекты и анимация | 4096 МБ | Папка `MapTemp` [кэша сборки](build.md#cache). <!-- src: Assets/Editor/MapBuilder.Storage.cs:158-182 --> |
| Метаданные: настройки карты, иконка, превью и текстуры мини-карты | 24 МБ | Папка `MetaTemp` кэша сборки. |

> [!IMPORTANT]
> Размер сборки проверяется только непосредственно перед созданием или обновлением публикации (кнопки **Создать публикацию** и **Обновить публикацию**), и результат появляется только в Console. Кнопки **Проверить карту**, **Собрать карту** и **Экспортировать в папку** (Export to folder) его не проверяют.

Оба размера измеряются по папкам кэша сборки, а не по упакованному `mod.cxmod`. Если какая-то часть слишком большая, ничего не загружается, и в Console появляется `'<scene>': the map is <X> mb and this vendor accepts <L> mb.`, для метаданных — с `the meta is`. На вкладке **Публикация** (Publish) размер публикации показывается относительно суммы обоих ограничений: `<MB> / 4120 mb`. <!-- src: Assets/Editor/MapBuilder.Publishing.cs:75-78 -->

У игры есть собственные ограничения на загрузку, поэтому очень большая карта может не загрузиться, даже если укладывается в ограничение mod.io. Прежде чем публиковать большую карту, загрузите её в игре; см. [Тест в игре](test-in-game.md).

<a name="limits-modio"></a>
### Настройки mod.io, которые поставляются с проектом

В проекте уже есть настройки mod.io для CarX Drift Racing Online 2 в файле `Assets/Resources/CarX.Modding/ModIoConfig.asset`, поэтому настраивать ничего не нужно. <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:15-34 -->

| Настройка | Значение |
| --- | --- |
| Игра | CarX Drift Racing Online 2, id игры `14124` |
| Страница игры | `https://mod.io/g/carx-dro2` |
| Язык по умолчанию | `en` |
| Размер карты | 4096 МБ |
| Размер метаданных | 24 МБ |
| Размер файла иконки | 8 МБ |
| Длина поля **Название** | 50 символов |
| Длина краткого описания | 250 символов |
| Длина поля **Описание** | 50 000 символов |

<a name="surfaces"></a>
## Дорожные поверхности и физические поля

Подменю **Road** в меню типа GameMarkerData предлагает 26 поверхностей в таком порядке. Маркер хранит свой тип как `Road/<surface>`, а игра загружает собственную поверхность по части после `Road/`. В последнем столбце указан **Surface Type** каждой поверхности в игре. <!-- src: Assets/Scripts/MarkerData.cs:12-37 -->

| № | Пункт меню | Сохраняемый тип | **Surface Type** в игре |
| --- | --- | --- | --- |
| 1 | **Road** > **default** | `Road/default` | Нет в игре |
| 2 | **Road** > **asphalt** | `Road/asphalt` | `Asphalt` |
| 3 | **Road** > **brick** | `Road/brick` | `Asphalt` |
| 4 | **Road** > **cobbles** | `Road/cobbles` | `Asphalt` |
| 5 | **Road** > **cobbles_big** | `Road/cobbles_big` | `Asphalt` |
| 6 | **Road** > **concrete** | `Road/concrete` | `Asphalt` |
| 7 | **Road** > **dirt** | `Road/dirt` | `Earth` |
| 8 | **Road** > **earth** | `Road/earth` | `Earth` |
| 9 | **Road** > **grass** | `Road/grass` | `Grass` |
| 10 | **Road** > **gravel** | `Road/gravel` | `Earth` |
| 11 | **Road** > **ice_snowy** | `Road/ice_snowy` | Нет в игре |
| 12 | **Road** > **kerb_racetrack** | `Road/kerb_racetrack` | `Asphalt` |
| 13 | **Road** > **liquid_water** | `Road/liquid_water` | `Snow` |
| 14 | **Road** > **metal** | `Road/metal` | `Earth` |
| 15 | **Road** > **metal_grille** | `Road/metal_grille` | `Earth` |
| 16 | **Road** > **pavement** | `Road/pavement` | Нет в игре |
| 17 | **Road** > **paving_slab** | `Road/paving_slab` | `Asphalt` |
| 18 | **Road** > **plastic** | `Road/plastic` | `Asphalt` |
| 19 | **Road** > **road_icy** | `Road/road_icy` | `Ice` |
| 20 | **Road** > **sand** | `Road/sand` | `Sand` |
| 21 | **Road** > **snow** | `Road/snow` | `Asphalt` |
| 22 | **Road** > **snow_asphalt** | `Road/snow_asphalt` | `Ice` |
| 23 | **Road** > **snow_cobbles** | `Road/snow_cobbles` | `Ice` |
| 24 | **Road** > **snow_gravel** | `Road/snow_gravel` | `Earth` |
| 25 | **Road** > **wood** | `Road/wood` | `Earth` |
| 26 | **Road** > **wood_plank** | `Road/wood_plank` | `Earth` |

> [!IMPORTANT]
> В игре нет поверхностей с именами `default`, `ice_snowy` и `pavement`, поэтому коллайдер с одной из них не получает дорожной поверхности — со значениями или без. Вместо `default` используйте `asphalt`, вместо `ice_snowy` — `road_icy`, а вместо `pavement` — `paving_slab`.

Такой коллайдер ведёт себя как коллайдер без маркера Road, а игра пишет в лог `[ModRoadSurfaceMaterials] Road marker references surface template '<name>', but no matching CARXSurfaceTemplate was found.` В примере TestMap у `Content/workshop_map_main/wall_coll` маркер `Road/default`, поэтому дорожной поверхности у него нет.

Маркер Road применяет свою поверхность только к тому GameObject, на котором находится, поэтому ставьте его на объект с коллайдером. См. [Разметка дорожных поверхностей](markers.md#road).

<a name="surface-values"></a>
### Когда применяются физические значения

Для каждого маркера Road игра загружает собственную поверхность с тем же именем. Если маркер экспортирует физические значения, они заменяют все значения этой поверхности, включая **Surface Type** и **Water Rate**. Если не экспортирует, игра использует свою поверхность без изменений. <!-- src: DR3PC1:Assets/Modules/CarX.Modding/Dots/ModRoadSurfaceMaterials.cs:44-85 -->

Сборка определяет значения каждого маркера по его **Template Config** и меню шаблона. Выделение маркеров в окне **Inspector** — по одному или нескольких сразу — не меняет того, что они экспортируют. Маркер экспортирует такие значения: <!-- src: Assets/Scripts/GameMarkerData.cs:10-38 -->

| Состояние маркера | Какие значения экспортирует маркер |
| --- | --- |
| Без **Template Config** | Никаких: игра использует собственную поверхность. Это относится и к маркеру, тип которого изменён кнопкой **Select : &lt;type&gt;**, и к значениям, которые сохранили на маркере окно **Inspector** или прежняя версия. |
| **Template Config** с именованным шаблоном | Значения шаблона на момент сборки. После изменения шаблона соберите карту заново; выделять его маркеры не нужно. |
| **Template Config** с **Custom** | Собственные значения маркера. Сначала это значения поверхности по умолчанию. При смене поверхности окно **Inspector** сбрасывает их к значениям по умолчанию новой поверхности, как только в следующий раз покажет маркер, поэтому после **Select : &lt;type&gt;** до этого момента остаются значения прежней поверхности. <!-- src: Assets/Editor/MarkerDataDrawer.cs:197-210 --> |
| **Template Config**, шаблон которого удалён | Собственные значения маркера **Custom**. Окно **Inspector** показывает в его меню шаблона **Custom**. |
| Значения, равные `0` во всех полях | Никаких: игра использует собственную поверхность. С **Friction** `0` поверхность стала бы скользкой, как лёд. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:555-560 --> |

Значения поверхности по умолчанию — это значения MapUploader из `Assets/Resources/DefaultRoadValues/<surface>.asset`; с них начинаются новый шаблон и маркер с **Custom**. Их **Surface Type** совпадает с типом этой поверхности в игре (для `default` и `pavement` это `Asphalt`, для `ice_snowy` — `Ice`), а **Water Rate** равен `0`, как у всех поверхностей игры. Остальные поля могут отличаться от поверхности игры: например, у `grass` **Bump Min** равен `0.05`, а в игре — `-0.03`, у `wood` **Friction** равен `0.85`, а в игре — `1`. Чтобы сохранить поверхность игры, не назначайте **Template Config**. <!-- src: Assets/Resources/DefaultRoadValues/grass.asset:15-25 -->

Поля появляются только после того, как вы назначите маркеру **Template Config**. Именованный шаблон показывает свои значения только для чтения, а **Custom** даёт маркеру собственные редактируемые значения. См. [Шаблоны значений дороги](markers.md#templates). <!-- src: Assets/Editor/MarkerDataDrawer.cs:141-190,212-225 -->

<a name="older-values"></a>
### Значения, сохранённые прежней версией

Прежняя версия проекта сохраняла значения дороги под другим именем типа. В этой версии они загружаются, в том числе в ваших старых сценах карт и наборах шаблонов, и подчиняются правилам выше. Когда вы сохраняете такую сцену или ассет, Unity записывает значения под текущим именем типа. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/Instances/PhysicMaterialProperties.cs:7-10 -->

Эти значения в проекте остались от прежней версии:

- Шаблон `AsphaultDef` в `Assets/Resources/GameMarkerTemplateConfig.asset`: **Friction** `1`, **Roll Friction** `0.01`, **Bump Scale** `100` и `0` во всех остальных полях, поэтому его **Surface Type** — `Asphalt`. Его используют маркеры `road` и `roadside` в примере TestMap, поэтому они экспортируют эти значения вместо поверхности `asphalt` игры. <!-- src: Assets/Resources/GameMarkerTemplateConfig.asset:46-58 -->
- `0` во всех полях у примера префаба `Road` и у маркеров `terrain`, `wall_coll` и `big_bw_strips (1)` в примере TestMap. У этих маркеров нет **Template Config**, поэтому они не экспортируют значений.

<a name="physics-fields"></a>
### Физические поля

Поля идут в таком порядке на маркере Road с **Template Config** и в каждом шаблоне GameMarkerTemplateConfig. Игра получает значения без изменений. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/Instances/PhysicMaterialProperties.cs:13-31 -->

| Поле | Диапазон или значения |
| --- | --- |
| **Friction** | от 0 до 5 |
| **Roll Friction** | от 0 до 1 |
| **Bump Min** | от −1 до 0.05 |
| **Bump Max** | от −0.2 до 1 |
| **Bump Scale** | от 0 до 100 |
| **Bump 2 Min** | от −1 до 0 |
| **Bump 2 Max** | от 0 до 1 |
| **Bump 2 Scale** | от −1 до 100 |
| **Water Rate** | Любое число |
| **Surface Type** | `Asphalt`, `Grass`, `Sand`, `Earth`, `Snow`, `Ice` или `Gravel` <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/Instances/SurfaceType.cs:5-11 --> |

**Surface Type** — поле внутри значений, у него 7 вариантов. Это не тот же список, что 26 поверхностей подменю **Road**.

<a name="optimization-defaults"></a>
## Настройки оптимизации сборки

Каждая карта хранит настройки оптимизации сборки в своих настройках карты (ассет MapMetaConfig). Большую часть из них показывает панель **Оптимизация** (Optimization) на вкладке **Сборка**, а размер сектора коллайдеров, предел треугольников коллайдера и наименьшее число треугольников сектора видны только в окне **Inspector** настроек карты, в разделе **Optimization**. Все настройки с их значениями по умолчанию и диапазонами, а также то, что оптимизация меняет и что оставляет без изменений, описаны в разделе [Оптимизация](build.md#optimization). О предупреждениях для больших мешей см. раздел [Бюджет сектора](#sector-budget). <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/BuildOptimizationSettings.cs:9-32 -->

<a name="lights"></a>
## Правила экспорта света

Оба формата экспортируют свет одинаково. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:87-123 -->

| Правило | Подробности |
| --- | --- |
| Экспортируемые типы | Только Point и Spot. Directional, Area и другие типы пропускаются с предупреждением в Console. |
| Какие источники | Включённый компонент Light на активном GameObject, у которого нет тега `Garbage` и который не находится внутри такого объекта. |
| Экспортируемые свойства | Положение, поворот и масштаб; тип; цвет; интенсивность; дальность; угол конуса и внутренний угол конуса; тип теней; ближняя плоскость теней; используется ли цветовая температура и её значение. |
| Интенсивность | Переводится в канделы, умножается на 0.1 и ограничивается значением 5000. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:71,78-85 --> |
| Лимит | Не больше 500 компонентов Light и 500 HDAdditionalLightData. Выключенные источники и источники на неактивных объектах тоже учитываются. |
| В игре | Экспортированные источники света включаются только ночью. <!-- src: DR3PC1:Assets/DR3/Scripts/States/Controllers/Mods/ModLightsDayNightBehaviour.cs:36-53 --> |
| Только для предпросмотра | Источники Directional и Area, Volume (небо, туман, экспозиция) и Reflection Probe освещают только предпросмотр в редакторе. Окружающее освещение игра задаёт сама. |
| Проверка | По одной заметке категории Lighting на каждый включённый источник Directional, например солнце для предпросмотра. По одному предупреждению категории Lighting на каждый включённый источник другого типа, кроме Point и Spot, например Area. Ещё одно предупреждение — если в сцене два или больше источника Directional, считая выключенные. <!-- src: Assets/Editor/Validation/SceneValidator.cs:713-760 --> |

Как настроить свет, см. в разделе [Настройка освещения](scene-setup.md#lighting).

<a name="materials"></a>
## Экспорт материалов HDRP

В таблице перечислены все свойства материала, которые читает экспортёр, в том порядке, в котором он их записывает. Wavefront и Binary используют одно и то же преобразование: сборка Binary читает тот же результат `.mtl`. Всё, чего нет в списке, например Detail Map, не экспортируется. Включать Read/Write для текстур материалов не нужно: экспорт включает его на то время, пока оно ему нужно. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlMaterialWriter.cs:30-74 -->

| Настройка HDRP | Свойство материала | Экспортируется как | Примечания |
| --- | --- | --- | --- |
| Запись маркера Material (только Binary) | — | Вариант render core (`cx_rc`) | Используется вместо преобразования Layered Lit. Wavefront маркер игнорирует. См. [Замена материалов вариантами render core](markers.md#material). |
| Слои Layered Lit | `_LayerCount` и свойства слоёв | Слоистый материал (`cx_pbr`) с 2–4 слоями | Каждый слой сохраняет Base Map, Normal Map, Mask Map, цвет, metallic, smoothness, масштаб нормалей и remapping; карта Layer Mask и смешивание по цвету вершин тоже сохраняются. Каждый слой должен использовать UV0. Смешивание по высоте (Height-based blend), режим плотности (density mode), влияние основного слоя (main-layer influence) или маска смешивания на UV, отличном от UV0, останавливают сборку с сообщением `Layered Lit '<material>': '<option>' is not supported by the mod material format.` <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/LayeredPbrWriter.cs:10-28 --> |
| Surface Type, Alpha Clipping | `_AlphaCutoffEnable`, очередь рендеринга, тег `RenderType`, `_Surface` | Режим смешивания: непрозрачный (`illum 2`), alpha test (`illum 1`) или alpha blend (`illum 4`) | Включённый Alpha Clipping даёт alpha test. Иначе тег `RenderType` со значением `TransparentCutout` даёт alpha test, а очередь рендеринга 3000 и выше или тег `Transparent` — alpha blend. Иначе `_Surface` со значением Transparent даёт alpha blend, а всё остальное — непрозрачный режим. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlMaterialWriter.cs:159-186 --> |
| Double-Sided, Double-Sided Normal Mode | `_DoubleSidedEnable` (или `_CullMode` / `_Cull` со значением Off), `_DoubleSidedNormalMode` | `ds` с Flip, Mirror или None | |
| Base Color | `_BaseColor` или `_BaseColor0` | Диффузный цвет `Kd`; для материалов alpha test и alpha blend его альфа становится `d` | |
| Base Map | `_BaseColorMap`, `_BaseColorMap0` или `_MainTex` | Диффузная карта `map_Kd`; материалы alpha test и alpha blend также получают карту альфы `map_d` | В карту альфы запекаются Alpha Remapping (только для материалов Transparent) и порог Alpha Clipping. |
| Normal Map, Normal Scale | `_NormalMap0` или `_NormalMap`, с `_NormalScale0` или `_NormalScale` | Карта нормалей в касательном пространстве `map_Bump` с запечённым масштабом | `_BumpMap` не читается. |
| Mask Map | `_MaskMap0` или `_MaskMap` | R → карта металличности `map_Pm`; A → карта шероховатости `map_Pr` (1 − smoothness) | G (ambient occlusion) и B (маска детализации) не экспортируются. Metallic Remapping и Smoothness Remapping игнорируются. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlTextureMapWriter.cs:122-153 --> |
| Metallic, Smoothness (без Mask Map) | `_Metallic`, `_Smoothness` или `_Glossiness` | Значения `Pm` и `Pr` (1 − smoothness) | |
| Emissive Color, Emission Map | `_EmissiveColor` (или `_EmissionColor` с ключевым словом `_EMISSION`), `_EmissiveColorMap` или `_EmissionMap` | Цвет свечения `Ke` и карта `map_Ke` | Экспортируются, только если цвет не чёрный. Чёрный цвет не даёт свечения, даже если карта задана. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlTextureMapWriter.cs:155-200 --> |
| Tiling, Offset | Масштаб и смещение каждого экспортируемого свойства текстуры | Параметры `-s` и `-o` соответствующей карты | |
| Тип текстуры | Каждое свойство текстуры из этой таблицы | Только Texture2D | Другой тип, например кубическая карта или Render Texture, пропускается с сообщением `[ObjExporter] Material '<m>' property '<p>' holds a <T>, only Texture2D can be exported. Skipped.` <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlTextureMapWriter.cs:378-395 --> |

Материалы Shader Graph и встроенного шейдера Standard экспортируют то, что они предоставляют под именами свойств из таблицы: по ссылочным именам (reference names), а не по отображаемым. Свойства `_Color`, `_BumpMap` и `_MetallicGlossMap` материала Standard не читаются. См. [Настройка материалов](scene-setup.md#materials).

<a name="menus"></a>
## Меню и ассеты, которые добавляет проект

| Пункт меню | Что делает | Кому нужен |
| --- | --- | --- |
| **Tools** > **MapBuilder** | Открывает окно **MapBuilder**. Если окно уже открыто, перезагружает список публикаций. | Каждому автору карт <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:133-144 --> |
| **Tools** > **CarX Modding** > **Create publisher configs** | Создаёт в `Assets/Resources/CarX.Modding` недостающие ассеты настроек площадок. Проект выполняет ту же проверку при каждой загрузке редактора, а оба ассета уже есть в проекте. | Интеграторам <!-- src: Assets/Editor/ModPublisherConfigSetup.cs:18,30-41 --> |
| **Tools** > **mod.io** > **Open Cache** | Открывает папку данных плагина mod.io в файловом менеджере. | Для работы с картами не нужен |
| **Tools** > **mod.io** > **Edit Settings** | Выделяет ассет настроек плагина mod.io и создаёт его как `Assets/Resources/mod.io/v3_config.asset`, если его нет. MapBuilder этот ассет не использует: он подключается с настройками из `ModIoConfig.asset`. | Для работы с картами не нужен |
| **ModSystem** > **Sync Publishing Defines** | Пересчитывает scripting defines слоя публикации: `CARX_MODDING_STEAM`, `CARX_MODDING_MODIO` и `CARX_MODDING_MODIO_UPLOAD_PROGRESS`. | Интеграторам |
| **ModSystem** > **Render Core** > **Regenerate Table**, **Update Lock From Project Render Core**, **Check** | Обслуживают зеркало render core, на котором основан маркер Material. Пункт **Update Lock From Project Render Core** в этом проекте неактивен, потому что ему нужен пакет render core игры. | Интеграторам <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RenderCoreMirrorUnity/RenderCoreMirrorMenu.cs:19-22 --> |
| **Map** > **Add vertex animation test to TestMap** | Создаёт `Assets/MapResources/TestMap/AnimationTest/` с анимированным тестовым флагом, добавляет три флага в пример сцены `TestMap` и сохраняет сцену. | Разработчикам инструмента |
| **Map** > **Enable physics on test flags**, **Map** > **Upgrade test flags to PBR cloth** | Изменяют тестовый флаг, который создаёт **Add vertex animation test to TestMap**. Пункту **Upgrade test flags to PBR cloth** нужны файлы текстур, которых нет в проекте. | Разработчикам инструмента |

> [!WARNING]
> Пункт **Map** > **Add vertex animation test to TestMap** сохраняет изменения в `Assets/MapResources/TestMap/TestMap.unity`. Не запускайте его, если хотите сохранить пример сцены без изменений. <!-- src: Assets/Editor/VertexAnimationTestScene.cs:97-98 -->

Проект также добавляет такие пункты в **Assets** > **Create**:

| Пункт меню | Что создаёт | Примечания |
| --- | --- | --- |
| **Assets** > **Create** > **Map** > **GameMarkerTemplateConfig** | Набор шаблонов дороги | Готовый набор поставляется как `Assets/Resources/GameMarkerTemplateConfig.asset`. См. [Шаблоны значений дороги](markers.md#templates). <!-- src: Assets/Scripts/GameMarkerTemplateConfig.cs:4 --> |
| **Assets** > **Create** > **Map** > **MapMetaConfig** | Настройки карты | Вместо этого используйте кнопку **+ Новая карта** (+ New map) в MapBuilder: она ещё и заполняет **Название** и выбирает карту. См. [Создание или выбор карты](map-settings.md#new-map). |
| **Assets** > **Create** > **Map** > **MapManagerConfig** | Ассет состояния MapBuilder | Не создавайте второй такой ассет. Из-за второй копии в любой папке `Resources` MapBuilder перестаёт работать с ошибкой `(Singleton) Multiple config for : MapManagerConfig`. <!-- src: Assets/Scripts/SingletonScriptableObject.cs:16-28 --> |
| **Assets** > **Create** > **Map** > **MapSkipComponentConfig** | Список скрытых заметок о компонентах | Не создавайте второй такой ассет; редактируйте поставляемый. См. [MapSkipComponentConfig](#skip-components). |
| **Assets** > **Create** > **CarX** > **Modding** > **mod.io Config**, **Steam Workshop Config** | Ассеты настроек площадок | Оба уже есть в `Assets/Resources/CarX.Modding`. См. [Площадки публикации](advanced/publishing-vendors.md). |
| **Assets** > **Create** > **Modio** > **v3** > **config**, **Modio** > **UI** > **ThemeSheet** | Ассеты плагина mod.io | Для работы с картами не нужны. |

<a name="skip-components"></a>
## MapSkipComponentConfig

`Assets/Resources/MapSkipComponentConfig.asset` перечисляет имена типов компонентов, о которых проверка не сообщает как о неподдерживаемых. В поставке в нём одна запись — `DrawZoneBehaviour`: вспомогательный компонент для гизмо на примерах префабов `SpawnPoint` и `Ambient`, а также на точке появления и мини-карте `TestMap`. <!-- src: Assets/Resources/MapSkipComponentConfig.asset:15-16 -->

- Запись только скрывает заметку категории Components `'<Type>' on '<object>' is not a supported component and will not be exported. …` Компонент по-прежнему не экспортируется. <!-- src: Assets/Editor/Validation/SceneValidator.cs:169-177 -->
- Запись не скрывает заметку категории Format о компонентах предпросмотра, заметку об Animator или SkinnedMeshRenderer без маркера Animation и заметку об отсутствующем скрипте.
- Запись — имя класса без пространства имён, например `AudioSource`. Оно должно совпадать точно, с учётом регистра.
- Список действует для всех карт проекта.
- Редактируйте поставляемый ассет, а не создавайте ещё один. Если в папках `Resources` два ассета MapSkipComponentConfig, проверка останавливается с сообщением `(Singleton) Multiple config for : MapSkipComponentConfig` в Console. <!-- src: Assets/Scripts/SingletonScriptableObject.cs:24-27 -->

Как добавить запись, см. в разделе [Скрытие заметок для типа компонента](validation.md#skip-types).

<a name="window-states"></a>
## Состояния окна MapBuilder

В этом разделе приведены тексты русского интерфейса. Английская версия страницы приводит тексты английского интерфейса. Почему элемент управления неактивен, см. в разделе [Неактивные элементы управления](troubleshooting.md#greyed-out).

### Шапка

| Текст аккаунта | Значение |
| --- | --- |
| `Аккаунт: <user>` | Вы вошли в mod.io. На кнопке аккаунта написано **Выйти** (Sign out). <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:537-548 --> |
| `Вход…` | Открыты диалоги входа. Кнопка неактивна. |
| `Не выполнен вход` | Проверка, сборка и экспорт доступны. Чтобы публиковать, нажмите **Войти** (Sign in). См. [Вход в mod.io](publish.md#sign-in). |
| `Недоступно` | mod.io ещё не инициализирован или не смог запуститься. Вкладка **Публикация** показывает причину в информационном блоке. |

Поле **Площадка** (Platform) в шапке всегда показывает `mod.io` и неактивно. Выпадающий список языка и область аккаунта неактивны, пока идёт сборка или загрузка.

### Карта не выбрана

| Где | Текст |
| --- | --- |
| Заголовок карты | `Выберите карту` <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:176 --> |
| Подзаголовок | `Создайте карту или выберите настройки существующей.` |
| Вкладки **Сборка** и **Публикация** | `Выберите локальную карту или добавьте новую.` |
| Нижняя строка | `Добавьте карту или выберите настройки существующей.` |
| Список `ЛОКАЛЬНЫЕ КАРТЫ` (`LOCAL MAPS`), когда в проекте нет настроек карт или ни одна карта не подходит под поиск | `Карт не найдено.` и `Добавьте карту или измените поиск.` |

### Строки списка ЛОКАЛЬНЫЕ КАРТЫ

В каждой строке показаны статус сборки и строка mod.io. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:179-214 -->

| Текст | Значение |
| --- | --- |
| `Готова к экспорту` | Map и Meta собраны, кэш сборки есть на диске, и со времени сборки не менялись ни вкладка **Карта** (Map), ни настройки оптимизации. |
| `Не собрана` | Не собрана ни одна часть: карту ни разу не собирали, или последняя сборка обеих частей завершилась неудачей. |
| `Нужна пересборка` | Любой другой случай: собрана только одна часть, изменилась настройка, после сборки была создана публикация или кэш сборки удалён. |
| `mod.io: нет связанной публикации` | С этой картой не связана ни одна публикация. |
| `mod.io: версия не загружена` | Публикация связана, но вы не вошли в аккаунт или её нет в списке, например потому что она удалена на mod.io. |
| `mod.io: нет активного файла` | У связанной публикации нет загруженного файла. |
| `mod.io: <version>` | Версия текущего файла связанной публикации. |
| `mod.io: версия не указана` | У текущего файла связанной публикации нет версии. |

### Вкладка «Сборка»

| Текст | Значение |
| --- | --- |
| `Геометрия и метаданные карты ещё не собраны.`, `Геометрия карты ещё не собрана.` или `Метаданные карты ещё не собраны.` | Нет одной или обеих частей. <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:824-833 --> |
| `Сборка устарела или её файлы недоступны. Соберите выбранную сцену в текущем формате.` | Обе части были собраны, но изменились **Сцена**, **Формат** (Format), вкладка **Карта** или настройки оптимизации, либо кэш сборки исчез. |
| `<dd.MM.yyyy HH:mm> · Собрано за <n> с · Кэш ресурсов: <n> МБ` | Последняя сборка прошла успешно. `Кэш ресурсов` — размер всей папки кэша сборки, а не размер мода. Дата появляется после повторного открытия окна. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:384-389 --> |
| `Сборка не завершена полностью. Проверьте результаты проверки и Console.` | После последней сборки по-прежнему нет Map или Meta: проверка нашла ошибки, либо сборка завершилась неудачей или была отменена. |
| `Соберите карту, чтобы увидеть состав сцены.` | У панели **Состав сцены** нет данных, потому что ещё ни одна сборка не включала цель **Map**. |
| `Для экспорта сначала соберите карту и её данные во вкладке «Сборка».` | Подсказка на вкладках **Сборка** и **Публикация**: сборка не соответствует значениям, выбранным в полях **Сцена** и **Формат**. |
| `Выберите папку для экспорта.` | Подсказка: сборка актуальна и в разделе **Куда отправить сборку** (Build destination) выбран вариант **Экспорт в папку** (Export to folder), но поле **Папка** (Folder) пустое. |

### Вкладка «Публикация»

| Текст | Значение |
| --- | --- |
| `Not signed in to https://mod.io/g/carx-dro2.` | Вы не вошли в аккаунт. Экспорт в папку по-прежнему работает. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoAuthProvider.cs:154 --> |
| `Соберите карту, чтобы создать публикацию. Для обновления выберите существующую публикацию ниже.` | В разделе **Куда отправить сборку** выбран вариант **Площадка**, и не выбрана связанная публикация. |
| `Выберите локальную карту.` | Подсказка для кнопки **Создать публикацию**: карта не выбрана. |
| `Сначала соберите карту и её данные. Публикация создаётся вместе с файлами.` | Подсказка для кнопки **Создать публикацию**: Map или Meta не собраны. |
| `В mod.io пока нет публикаций.` и `Соберите карту и нажмите «Создать публикацию».` | Список пуст. Этот текст показывается и когда вы не вошли в аккаунт, потому что тогда ничего не загружается. |

Тексты в строках публикаций, например `Blank <n>`, `<MB> / 4120 mb`, `Old version!` и `Выбрать для этой карты` (`Select for this map`), описаны в разделе **Список «Публикации»** (Publications) на странице [Публикация на mod.io](publish.md).

### Нижняя строка

После операции в нижней строке окна показывается один из этих текстов. <!-- src: Assets/Editor/MapBuilderEditorWindow.Publish.cs:101-105,139-141,182-184 -->

| Текст | После |
| --- | --- |
| `Локальная карта: <name>` | Выбора карты |
| `Проверка завершена. Результаты — в окне проверки карты.` | Нажатия **Проверить карту** |
| `Собрано за <n> с · Кэш ресурсов: <n> МБ` или `Сборка не завершена полностью. Проверьте результаты проверки и Console.` | Нажатия кнопки **Собрать карту** |
| `Экспортировано в <path>` или `Нечего экспортировать — подробности в Console.` | Нажатия кнопки **Экспортировать в папку** |
| `Публикация создана.` или `Публикация не завершена. Подробности в Console.` | Нажатия **Создать публикацию** |
| `Публикация обновлена.` или `Обновление не завершено. Подробности в Console.` | Нажатия **Обновить публикацию** |

<a name="storage"></a>
## Где хранятся настройки и сборки

| Что | Где | Переносится вместе с папкой проекта |
| --- | --- | --- |
| Поля вкладки **Карта**, ID карты, настройки оптимизации | Настройки карты (ассет MapMetaConfig) там, где вы их сохранили, например в `Assets/MapResources/<MapName>/` | Да |
| Выбранная карта, связи с публикациями, записи о сборках, списки изменений, последняя **Сцена** и флажки **Обновить название** (Update title), **Обновить описание** и **Обновить превью** (Update preview) | `Assets/Resources/MapManagerConfig.asset` | Да <!-- src: Assets/Editor/MapManagerConfig.cs:24-32 --> |
| Шаблоны дороги | `Assets/Resources/GameMarkerTemplateConfig.asset` или созданный вами набор | Да |
| Скрытые заметки о компонентах | `Assets/Resources/MapSkipComponentConfig.asset` | Да |
| Подключение к mod.io и ограничения | `Assets/Resources/CarX.Modding/ModIoConfig.asset` | Да |
| Кэш сборки | `<temp>/DefaultCompany/ModeMap/StandaloneTemporary<map id>/MapTemp` и `/MetaTemp`. В Windows `<temp>` — обычно `%USERPROFILE%\AppData\Local\Temp`. | Нет: соберите карту заново на другом компьютере <!-- src: Assets/Editor/MapBuilder.Storage.cs:11-14,64-83 --> |
| Язык интерфейса | Ключ EditorPrefs `CarX.MapBuilder.Language`: `en` (по умолчанию) или `ru` | Нет <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:14,58 --> |
| Сводка последней сборки карты | Ключ EditorPrefs `CarX.MapBuilder.BuildSummary.<map id>` | Нет |
| **Текстуры Binary** (Binary textures) | Ключ EditorPrefs `CarX.Modding.BinaryTextureEncoding`: по умолчанию BC7; действует для всех карт | Нет <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/BinaryMapPacker.cs:14-20 --> |
| Email для mod.io и принятые условия | Ключи EditorPrefs `CarX.Modding.Publisher.Email.modio` и `CarX.Modding.Publisher.Terms.modio` | Нет <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/Publishing/ModAuthPreferences.cs:16-17 --> |
| Токен входа в mod.io | На диске, его сохраняет плагин mod.io | Нет: войдите заново |
| **Куда отправить сборку** и **Папка** | Само окно MapBuilder; при закрытии окна они сбрасываются | Нет |

EditorPrefs относятся к вашей учётной записи пользователя на этом компьютере и общие для всех проектов Unity. ID карты — это GUID ассета настроек карты; MapUploader задаёт его автоматически.

<a name="glossary"></a>
## Глоссарий

Используйте эти термины в обоих языках. В столбце **Значение** указано, где объясняется каждый термин.

| EN | RU | Значение |
| --- | --- | --- |
| Animation marker | маркер Animation | Маркер, который запекает клипы Animator в вершинную анимацию. См. [Вершинная анимация объектов](markers.md#animation). |
| budget | лимит компонентов | Допустимое количество компонентов одного типа на карте. См. [Поддерживаемые компоненты и лимиты](#budgets). |
| build | сборка / собрать | Превращение сцены и настроек карты в файлы в кэше сборки кнопкой **Собрать карту**. См. [Сборка карты](build.md). |
| build cache | кэш сборки | Папка `StandaloneTemporary<map id>` с `MapTemp` и `MetaTemp`, куда записывается сборка. Это не пакет для игры. См. [Куда записывается сборка](build.md#cache). |
| changelog | список изменений | Текст, который отправляется с каждым загруженным файлом; вводится в поле **Что изменилось** (Changelog). См. [Обновление публикации](publish.md#update). |
| collider | коллайдер | Mesh, Box, Sphere или Capsule Collider, который даёт объекту коллизию. См. [Добавление коллайдеров](scene-setup.md#colliders). |
| dynamic object (Rigidbody) | динамический объект (Rigidbody) | Объект с Rigidbody, который симулирует игра. См. [Добавление динамических объектов (Rigidbody)](scene-setup.md#dynamic). |
| error / warning / note | ошибка / предупреждение / заметка | Три уровня важности проверки. Сборку останавливают только ошибки. См. [Уровни важности](validation.md#severities). |
| export | экспорт | Копирование актуальной сборки в папку кнопкой **Экспортировать в папку**. См. [Экспорт карты в папку игры](test-in-game.md#export). |
| format (Binary, Wavefront) | формат (Binary, Wavefront) | Способ хранения сборки: Binary упаковывает её в один файл `mod.cxmod`, Wavefront оставляет отдельные файлы. См. [Выбор сцены и формата](build.md#format). |
| game folder | папка игры | Папка с исполняемым файлом игры на ПК. Локальные копии карт попадают в её подпапку `Mods`. См. [Где игра ищет локальные карты](test-in-game.md#local-maps). |
| `Garbage` tag | тег `Garbage` | Тег, который исключает объект и его дочерние объекты из проверки, экспорта и оптимизации. См. [Организация сцены](scene-setup.md#garbage). |
| icon | иконка | Изображение **Иконка · 16:9**: логотип на mod.io и изображение карты в игре. См. [Требования к изображениям](minimap-and-images.md#images). |
| linked publication | связанная публикация | Публикация, привязанная к одной локальной карте. Чтобы связать её, выберите её строку в списке **Публикации**. См. [Обновление публикации](publish.md#update). |
| local copy | локальная копия | Карта, экспортированная в `<game folder>/Mods/<MapName>/` для тестирования. См. [Тест в игре](test-in-game.md). |
| local map | локальная карта | Карта в списке `ЛОКАЛЬНЫЕ КАРТЫ` окна MapBuilder, то есть настройки карты в этом проекте. |
| LOD group | группа LOD | Компонент LODGroup с числом уровней детализации до 8. См. [Использование LOD-групп](scene-setup.md#lod). |
| map | карта | Гоночная трасса, сделанная в этом проекте: сцена и её настройки карты. |
| Map and Meta targets | цели Map и Meta | Две части сборки. Map содержит геометрию, коллизию, маркеры, свет, динамические объекты и анимацию; Meta — настройки карты, иконку, превью и мини-карту. |
| map config | настройки карты | Ассет MapMetaConfig с полями вкладки **Карта**, настройками оптимизации и ID карты. Его создаёт кнопка **+ Новая карта**. См. [Создание или выбор карты](map-settings.md#new-map). |
| map id | ID карты | GUID ассета настроек карты. По нему называется кэш сборки, и сборка несёт его в своих метаданных, пока вы не свяжете публикацию и не соберёте карту заново. |
| marker | маркер | Компонент GameMarkerData с типом: SpawnPoint, Road, Animation или Material. См. [Маркеры GameMarkerData](markers.md). |
| Material marker | маркер Material | Маркер, который в сборках Binary заменяет материалы объекта вариантами render core. См. [Замена материалов вариантами render core](markers.md#material). |
| metadata | метаданные | Часть Meta сборки. См. *цели Map и Meta*. |
| minimap | мини-карта | Изображение карты сверху из компонента Minimap. См. [Добавление мини-карты](minimap-and-images.md#minimap). |
| mod | мод | Контент, который игра загружает из папки мода или с mod.io. Карта — один из видов модов. |
| mod ID (the mod folder name) | ID мода (имя папки мода) | То, по чему игра различает моды: локальный мод — по имени папки, установленный с mod.io — как `<modIoId>_<modfileId>`. |
| `mod.cxmod` | `mod.cxmod` | Единственный файл, в который упаковывается сборка Binary при экспорте или загрузке. |
| optimization (build optimization) | оптимизация сборки | Этап сборки, который режет статическую геометрию на секторы, объединяет её и добавляет LOD. См. [Оптимизация](build.md#optimization). |
| preview | превью | Изображение **Превью · 16:9** — крупная картинка, которая хранится в метаданных карты. См. [Требования к изображениям](minimap-and-images.md#images). |
| publication | публикация | Запись мода на mod.io, показанная в списке **Публикации**. См. [Публикация на mod.io](publish.md). |
| render core variant | вариант render core | Один из типов материалов игры, который может назначить маркер Material. |
| Road marker | маркер Road | Маркер, который даёт объекту одну из 26 дорожных поверхностей. См. [Дорожные поверхности и физические поля](#surfaces). |
| sector | сектор | Одна из ячеек сетки, на которые оптимизация режет статическую геометрию. |
| spawn point / fast-travel point | точка появления / точка быстрого перемещения | Маркер SpawnPoint. По умолчанию машина появляется на первом из них, и каждый служит точкой быстрого перемещения на карте мира в игре. См. [Добавление точек появления](markers.md#spawn). |
| surface | поверхность | Одна из 26 заготовок Road, например `Road/asphalt`. Не то же самое, что поле **Surface Type**. |
| template | шаблон | Именованный набор физических значений дороги в GameMarkerTemplateConfig. См. [Шаблоны значений дороги](markers.md#templates). |
| validation | проверка | Проверка карты по всем правилам — кнопкой **Проверить карту** или первым этапом сборки. См. [Проверка карты](validation.md). |
| vertex animation | вершинная анимация | Анимация, запечённая из клипов Animator, которую игра воспроизводит без Animator. См. [Вершинная анимация объектов](markers.md#animation). |

## Дальнейшие шаги

Формат файлов сборки и подключение площадок публикации описаны на страницах [Формат мода: Wavefront и Binary](advanced/mod-format.md) и [Площадки публикации (для интеграторов)](advanced/publishing-vendors.md).

← [Решение проблем](troubleshooting.md) · [Содержание](README.md) · [Формат мода: Wavefront и Binary](advanced/mod-format.md) →
