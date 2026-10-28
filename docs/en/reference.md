# Reference

**English** · [Русский](../ru/reference.md)

Look up the budgets, limits, road surfaces, export rules, menus and saved state of MapUploader in one place. The task pages explain how to use each of them, and the [glossary](#glossary) gives every term in English and Russian.

<a name="budgets"></a>
## Supported components and budgets

Validation counts the components of every object in the scene selected in **Scene** on the **Build** tab, including inactive objects and disabled components. It skips objects tagged `Garbage` with their children, and hidden helper objects that aren't saved with the scene. A count outside its range is a Budget error, and errors stop the build. <!-- src: Assets/Editor/Validation/SceneValidator.cs:77-121,185-211 -->

| Component | Min | Max | Notes |
| --- | --- | --- | --- |
| Transform | 1 | 20 000 | Every GameObject has one, so this is the object count. <!-- src: Assets/Editor/MapSceneRules.cs:12 --> |
| MeshCollider | 0 | 10 000 | Static collision, or the collision of a dynamic object. See [Add colliders](scene-setup.md#colliders). |
| BoxCollider | 0 | 10 000 | Static or dynamic collision. |
| SphereCollider | 0 | 1000 | Static or dynamic collision. |
| CapsuleCollider | 0 | 1000 | Static or dynamic collision. |
| Rigidbody | 0 | 1000 | A [dynamic object](scene-setup.md#dynamic). |
| MeshRenderer | 0 | 10 000 | Visible geometry, together with its MeshFilter. |
| MeshFilter | 0 | 10 000 | The mesh of a MeshRenderer. |
| Light | 0 | 500 | Every light counts, including Directional and disabled lights, but only Point and Spot lights are exported. See [Light export rules](#lights). |
| HDAdditionalLightData | 0 | 500 | HDRP adds one to every light. |
| LODGroup | 0 | 10 000 | A [LOD group](scene-setup.md#lod). |
| GameMarkerData | 1 | 10 000 | A [marker](markers.md#types). The map needs at least one, because it needs a spawn point. |
| Minimap | 1 | 1 | The [minimap](minimap-and-images.md#minimap): exactly one. <!-- src: Assets/Editor/MapSceneRules.cs:24 --> |

Components that aren't in the table are reported as notes, which never stop the build: <!-- src: Assets/Editor/Validation/SceneValidator.cs:123-183 -->

- Volume, Reflection Probe and HDAdditionalReflectionData give a Format note. They light only the preview in the editor.
- Animator and SkinnedMeshRenderer are accepted inside the hierarchy of an Animator that has an [Animation marker](markers.md#animation). Anywhere else they give a Components note and aren't exported.
- Every other component gives a Components note and isn't exported, for example Camera, CaptureCamera, AudioSource, particle systems, Terrain and TerrainCollider. Types listed in [MapSkipComponentConfig](#skip-components) give no note.
- A missing script gives a Components note, and its component slot is skipped.

Other limits on scene content:

| Rule | Limit | What happens otherwise |
| --- | --- | --- |
| Levels in one LOD group | 8 | A Geometry error in validation, which stops the build. <!-- src: Assets/Editor/Validation/SceneValidator.cs:811-819 --> |
| **Bake frames per second** of an Animation marker | 1–60, default 30 | The **Inspector** slider doesn't go outside the range. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/Meta/AnimationMeta.cs:12 --> |
| Animation markers per Animator | 1 | The build stops with `Animator '<name>' has multiple Animation markers.` in the Console. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:34-35 --> |
| Marked Animators inside each other | Not allowed | The build stops with `Animation marker hierarchies must not overlap.` in the Console. |
| Rigidbody inside a marked Animator's hierarchy | Only on the Animator root or above it | The build stops with `Put Rigidbody on the Animator root or above it; a baked animation cannot contain independent moving bodies.` in the Console. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:45-46 --> |

After a build that includes **Map**, the **Scene contents** panel on the **Build** tab lists, in this form, the count of each component type that the scene contains. Types with a count of 0 aren't listed. See [Build the map](build.md). <!-- src: Assets/Scripts/ValidItemData.cs:29-35,95-98 -->

```text
<Type> : <n> count (<min> min - <max> max)
```

<a name="sector-budget"></a>
### Sector budget

Validation also gives a Geometry warning for each large static mesh that build optimization won't cut into sectors. These warnings never stop the build. <!-- src: Assets/Editor/Validation/SceneValidator.cs:853-883 -->

- **What is checked.** Each enabled MeshRenderer that has a MeshFilter, and each enabled MeshCollider that isn't a trigger and isn't convex, on an object that optimization processes: an active object that isn't tagged `Garbage` and has no Rigidbody, Animator, Animation, LODGroup or marker other than Road and Material on itself or on a parent. <!-- src: Assets/Editor/Validation/SceneValidator.cs:879-881 -->
- **The budget.** A mesh exceeds it when it is larger than **Sector size (m)** in any world dimension, `64` m by default, or has more triangles than **Max triangles per mesh**, `8192` by default. Colliders are measured against the same two values, not against the collider grid.
- **When the warning appears.** Only for meshes that optimization won't cut into sectors: every such mesh while **Optimize build** is off, MeshColliders while **Partition static colliders** is off, and render meshes while **Partition static render meshes** is off. With the default settings, there are no sector-budget warnings, and the TestMap sample gets none.
- **What isn't listed.** A renderer or MeshCollider that optimization leaves whole for another reason, for example a transparent renderer or an object with more than one MeshCollider, keeps its size and gets no warning. See [Optimization](build.md#optimization).

<a name="limits"></a>
## Limits

**Validate map** and the **Build map** button check the [map settings](#limits-settings) and [images](#limits-images) against these limits, and an error stops the build. The [build size](#limits-size) is checked only when you publish. Most of the limits come from the [mod.io settings that ship with the project](#limits-modio).

<a name="limits-settings"></a>
### Map settings

| Field | Limit | Notes |
| --- | --- | --- |
| Scene name | Starts with a letter; only letters, digits and `_` | The file name of the scene selected in **Scene**, without `.unity`. <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:32-45 --> |
| **Name** | Required; at most 50 characters | Letters of any alphabet, digits, spaces and `- _ ' . , : ! ? ( ) & + /`, with no space at the start or end. Shown in the game and on mod.io. <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:8-30 --> |
| **Version** | None | When empty, `1.0.0` is used. It becomes the version of each file uploaded to mod.io and is stored in the map metadata. <!-- src: Assets/Editor/MapBuilder.cs:170 --> |
| **Summary (optional)** | At most 250 characters | Only the first line is used, and the limit applies to it. When **Summary (optional)** is empty, the first line of **Description** is used; when both are empty, `<Name> track.` is used. <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:47-60 --> |
| **Description** | At most 50 000 characters | Stored in the map metadata. Sent to mod.io by **Create publication** when it isn't empty, and by **Update publication** with **Update description** on. See [Create a publication](publish.md#create) and [Update a publication](publish.md#update). |
| **Authors** | None | The game shows up to 5 names joined with `, `, then `...`. With no authors, it shows the name of the mod.io account that created the publication. <!-- src: DR3PC1:Assets/DR3/Scripts/States/Controllers/Mods/ModAuthorResolver.cs:10-31 --> |
| **URL** | None | Stored in the map metadata. |

For what to enter in each field, see [Fill in the Map tab](map-settings.md#map-tab).

<a name="limits-images"></a>
### Images

| Image | Required | Format | Read/Write | File size | Pixel size |
| --- | --- | --- | --- | --- | --- |
| **Icon · 16:9** | Yes: an error when missing | PNG. JPG gives a warning, any other format an error. | Must be on | At most 8 MB | mod.io needs at least 512 × 288 and recommends 16:9. MapUploader doesn't check it. <!-- src: Assets/Plugins/Modio/Modio/API/Generated/Definitions/RequestObjects/EditModRequest.cs:31 --> |
| **Preview · 16:9** | No: a warning when missing | PNG. JPG gives a warning, any other format an error. | Must be on | At most 10 MB | Not checked; 16:9 is recommended. <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:14 --> |
| Minimap texture | Yes: at least one **Textures** element, each with a **Main Texture** | Texture2D; any other texture type is an error | Not needed | Counts toward the metadata size | Not checked. The game uses only **Element 0**. |

The Icon is both the mod.io logo and the image of the map in the game. The Preview isn't uploaded as the logo; it travels inside the map metadata. See [Image requirements](minimap-and-images.md#images).

<a name="limits-size"></a>
### Build size

| Part | Limit | Measured on |
| --- | --- | --- |
| Map: geometry, collision, markers, lights, dynamic objects and animation | 4096 MB | The `MapTemp` folder of the [build cache](build.md#cache). <!-- src: Assets/Editor/MapBuilder.Storage.cs:158-182 --> |
| Metadata: map settings, Icon, Preview and minimap textures | 24 MB | The `MetaTemp` folder of the build cache. |

> [!IMPORTANT]
> The build size is checked only immediately before **Create publication** or **Update publication**, and the result appears only in the Console. **Validate map**, the **Build map** button and the **Export to folder** button don't check it.

Both sizes are measured on the build cache folders, not on the packed `mod.cxmod`. When a part is too large, nothing is uploaded, and the Console shows `'<scene>': the map is <X> mb and this vendor accepts <L> mb.`, or `the meta is` for the metadata. On the **Publish** tab, a publication's size is shown against the sum of both limits: `<MB> / 4120 mb`. <!-- src: Assets/Editor/MapBuilder.Publishing.cs:75-78 -->

The game has load limits of its own, so a very large map may fail to load even within the mod.io limit. Load a large map in the game before you publish it; see [Test the map in the game](test-in-game.md).

<a name="limits-modio"></a>
### mod.io settings that ship with the project

The project already contains the mod.io settings for CarX Drift Racing Online 2 in `Assets/Resources/CarX.Modding/ModIoConfig.asset`, so you don't configure anything. <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:15-34 -->

| Setting | Value |
| --- | --- |
| Game | CarX Drift Racing Online 2, game id `14124` |
| Game page | `https://mod.io/g/carx-dro2` |
| Default language | `en` |
| Map size | 4096 MB |
| Metadata size | 24 MB |
| Icon file size | 8 MB |
| **Name** length | 50 characters |
| Summary length | 250 characters |
| **Description** length | 50 000 characters |

<a name="surfaces"></a>
## Road surfaces and physics fields

The **Road** submenu of the GameMarkerData type menu offers 26 surfaces, in this order. A marker stores its type as `Road/<surface>`, and the game loads its own surface by the part after `Road/`. The last column gives the **Surface Type** of each surface in the game. <!-- src: Assets/Scripts/MarkerData.cs:12-37 -->

| # | Menu entry | Stored type | **Surface Type** in the game |
| --- | --- | --- | --- |
| 1 | **Road** > **default** | `Road/default` | No game surface |
| 2 | **Road** > **asphalt** | `Road/asphalt` | `Asphalt` |
| 3 | **Road** > **brick** | `Road/brick` | `Asphalt` |
| 4 | **Road** > **cobbles** | `Road/cobbles` | `Asphalt` |
| 5 | **Road** > **cobbles_big** | `Road/cobbles_big` | `Asphalt` |
| 6 | **Road** > **concrete** | `Road/concrete` | `Asphalt` |
| 7 | **Road** > **dirt** | `Road/dirt` | `Earth` |
| 8 | **Road** > **earth** | `Road/earth` | `Earth` |
| 9 | **Road** > **grass** | `Road/grass` | `Grass` |
| 10 | **Road** > **gravel** | `Road/gravel` | `Earth` |
| 11 | **Road** > **ice_snowy** | `Road/ice_snowy` | No game surface |
| 12 | **Road** > **kerb_racetrack** | `Road/kerb_racetrack` | `Asphalt` |
| 13 | **Road** > **liquid_water** | `Road/liquid_water` | `Snow` |
| 14 | **Road** > **metal** | `Road/metal` | `Earth` |
| 15 | **Road** > **metal_grille** | `Road/metal_grille` | `Earth` |
| 16 | **Road** > **pavement** | `Road/pavement` | No game surface |
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
> The game has no surface named `default`, `ice_snowy` or `pavement`, so a collider with one of them gets no road surface, with values or without. Use `asphalt` instead of `default`, `road_icy` instead of `ice_snowy` and `paving_slab` instead of `pavement`.

Such a collider behaves like a collider without a Road marker, and the game logs `[ModRoadSurfaceMaterials] Road marker references surface template '<name>', but no matching CARXSurfaceTemplate was found.` In the TestMap sample, `Content/workshop_map_main/wall_coll` has a `Road/default` marker, so it gets no road surface.

A Road marker applies its surface only to the GameObject it is on, so put it on the object with the collider. See [Mark road surfaces](markers.md#road).

<a name="surface-values"></a>
### When the physics values apply

For each Road marker, the game loads its own surface of the same name. If the marker exports physics values, they replace all the values of that surface, **Surface Type** and **Water Rate** included. If it exports none, the game uses its surface unchanged. <!-- src: DR3PC1:Assets/Modules/CarX.Modding/Dots/ModRoadSurfaceMaterials.cs:44-85 -->

The build resolves the values of each marker from its **Template Config** and its template menu. Selecting markers in the **Inspector**, one at a time or several at once, doesn't change what they export. A marker exports these values: <!-- src: Assets/Scripts/GameMarkerData.cs:10-38 -->

| Marker state | Values the marker exports |
| --- | --- |
| No **Template Config** | None: the game uses its own surface. This also applies to a marker whose type you changed with **Select : &lt;type&gt;**, and to values that the **Inspector** or an earlier version saved on the marker. |
| **Template Config** with a named template | The template's values at the time of the build. After you edit a template, build the map again; its markers don't need to be selected. |
| **Template Config** with **Custom** | The marker's own values. They start from the surface defaults. When the surface changes, the **Inspector** resets them to the defaults of the new surface the next time it shows the marker, so after **Select : &lt;type&gt;** they keep the old surface's values until then. <!-- src: Assets/Editor/MarkerDataDrawer.cs:197-210 --> |
| **Template Config** whose template was removed | The marker's own **Custom** values. The **Inspector** shows **Custom** in its template menu. |
| Values that are `0` in every field | None: the game uses its own surface. **Friction** `0` would make the surface as slippery as ice. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:555-560 --> |

The surface defaults are the MapUploader values in `Assets/Resources/DefaultRoadValues/<surface>.asset`, and a new template and a **Custom** marker start from them. Their **Surface Type** matches the game's type for that surface (`Asphalt` for `default` and `pavement`, `Ice` for `ice_snowy`), and their **Water Rate** is `0`, as on every game surface. Their other fields can differ from the game's surface: for example, `grass` has **Bump Min** `0.05`, against `-0.03` in the game, and `wood` has **Friction** `0.85`, against `1`. To keep the game's surface, don't assign a **Template Config**. <!-- src: Assets/Resources/DefaultRoadValues/grass.asset:15-25 -->

The fields appear only after you assign a **Template Config** to the marker. A named template shows its values read-only, and **Custom** gives the marker its own editable values. See [Reuse road values with templates](markers.md#templates). <!-- src: Assets/Editor/MarkerDataDrawer.cs:141-190,212-225 -->

<a name="older-values"></a>
### Values saved by an earlier version

An earlier version of the project saved road values under another type name. They load in this version, in your older map scenes and template configs too, and follow the rules above. When you save such a scene or asset, Unity stores the values under the current type name. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/Instances/PhysicMaterialProperties.cs:7-10 -->

These values in the project come from an earlier version:

- The `AsphaultDef` template in `Assets/Resources/GameMarkerTemplateConfig.asset`: **Friction** `1`, **Roll Friction** `0.01`, **Bump Scale** `100` and `0` in every other field, so its **Surface Type** is `Asphalt`. The TestMap markers `road` and `roadside` use it, so they export these values instead of the game's `asphalt`. <!-- src: Assets/Resources/GameMarkerTemplateConfig.asset:46-58 -->
- `0` in every field on the `Road` example prefab and on the TestMap markers `terrain`, `wall_coll` and `big_bw_strips (1)`. These markers have no **Template Config**, so they export no values.

<a name="physics-fields"></a>
### Physics fields

The fields appear in this order on a Road marker with a **Template Config**, and on each template of a GameMarkerTemplateConfig. The game receives the values as they are. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/Instances/PhysicMaterialProperties.cs:13-31 -->

| Field | Range or values |
| --- | --- |
| **Friction** | 0 to 5 |
| **Roll Friction** | 0 to 1 |
| **Bump Min** | −1 to 0.05 |
| **Bump Max** | −0.2 to 1 |
| **Bump Scale** | 0 to 100 |
| **Bump 2 Min** | −1 to 0 |
| **Bump 2 Max** | 0 to 1 |
| **Bump 2 Scale** | −1 to 100 |
| **Water Rate** | Any number |
| **Surface Type** | `Asphalt`, `Grass`, `Sand`, `Earth`, `Snow`, `Ice` or `Gravel` <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/Instances/SurfaceType.cs:5-11 --> |

**Surface Type** is a field inside the values, with 7 options. It isn't the same list as the 26 surfaces of the **Road** submenu.

<a name="optimization-defaults"></a>
## Build optimization settings

Each map stores its build optimization settings in its map config. The **Optimization** panel on the **Build** tab shows most of them; the collider sector size, the collider triangle limit and the minimum sector triangles appear only in the **Inspector** of the map config, under **Optimization**. For every setting with its default and range, and for what optimization changes and what it leaves as it is, see [Optimization](build.md#optimization). For the warnings about large meshes, see [Sector budget](#sector-budget). <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/BuildOptimizationSettings.cs:9-32 -->

<a name="lights"></a>
## Light export rules

Both formats export lights the same way. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:87-123 -->

| Rule | Detail |
| --- | --- |
| Exported types | Point and Spot lights only. Directional, Area and other types are skipped with a Console warning. |
| Which lights | An enabled Light component on an active GameObject that isn't tagged `Garbage` or inside such an object. |
| Exported properties | Position, rotation and scale; type; color; intensity; range; spot angle and inner spot angle; shadow type; shadow near plane; whether color temperature is used, and the color temperature. |
| Intensity | Converted to candela, multiplied by 0.1 and capped at 5000. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:71,78-85 --> |
| Budget | At most 500 Light and 500 HDAdditionalLightData components. Disabled lights and lights on inactive objects count too. |
| In the game | Exported lights turn on only at night. <!-- src: DR3PC1:Assets/DR3/Scripts/States/Controllers/Mods/ModLightsDayNightBehaviour.cs:36-53 --> |
| Preview only | Directional and Area lights, Volume (sky, fog, exposure) and Reflection Probes light only the preview in the editor. The game supplies its own environment lighting. |
| Validation | One Lighting note for each enabled Directional light, such as a preview sun. One Lighting warning for each enabled light of another type that isn't a Point or Spot light, such as an Area light. One more warning when the scene has two or more Directional lights, disabled ones included. <!-- src: Assets/Editor/Validation/SceneValidator.cs:713-760 --> |

For how to set up lights, see [Set up lighting](scene-setup.md#lighting).

<a name="materials"></a>
## HDRP material export

The table lists every material property the exporter reads, in the order it writes them. Wavefront and Binary use the same conversion: a Binary build reads the same `.mtl` output. Anything not listed, such as a Detail Map, isn't exported. You don't need to turn on Read/Write for material textures; export turns it on for as long as it needs. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlMaterialWriter.cs:30-74 -->

| HDRP setting | Material property | Exported as | Notes |
| --- | --- | --- | --- |
| Material marker entry (Binary only) | — | Render core variant (`cx_rc`) | Used instead of the Layered Lit conversion. Wavefront ignores the marker. See [Replace materials with render core variants](markers.md#material). |
| Layered Lit layers | `_LayerCount` and the layer properties | Layered material (`cx_pbr`) with 2–4 layers | Each layer keeps its Base Map, Normal Map, Mask Map, color, metallic, smoothness, normal scale and remapping; the Layer Mask map and vertex-color blending are kept too. Every layer must use UV0. Height-based blend, density mode, main-layer influence or a blend mask on a UV other than UV0 stops the build with `Layered Lit '<material>': '<option>' is not supported by the mod material format.` <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/LayeredPbrWriter.cs:10-28 --> |
| Surface Type, Alpha Clipping | `_AlphaCutoffEnable`, render queue, `RenderType` tag, `_Surface` | Blend mode: opaque (`illum 2`), alpha test (`illum 1`) or alpha blend (`illum 4`) | Alpha Clipping on gives alpha test. Otherwise the `RenderType` tag `TransparentCutout` gives alpha test, and a render queue of 3000 or more or the tag `Transparent` gives alpha blend. Otherwise `_Surface` set to Transparent gives alpha blend, and anything else is opaque. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlMaterialWriter.cs:159-186 --> |
| Double-Sided, Double-Sided Normal Mode | `_DoubleSidedEnable` (or `_CullMode` / `_Cull` set to Off), `_DoubleSidedNormalMode` | `ds` with Flip, Mirror or None | |
| Base Color | `_BaseColor` or `_BaseColor0` | Diffuse color `Kd`; its alpha becomes `d` for alpha-test and alpha-blend materials | |
| Base Map | `_BaseColorMap`, `_BaseColorMap0` or `_MainTex` | Diffuse map `map_Kd`; alpha-test and alpha-blend materials also get an alpha map `map_d` | The alpha map has Alpha Remapping (Transparent materials only) and the Alpha Clipping threshold baked in. |
| Normal Map, Normal Scale | `_NormalMap0` or `_NormalMap`, with `_NormalScale0` or `_NormalScale` | Tangent-space normal map `map_Bump`, with the scale baked in | `_BumpMap` isn't read. |
| Mask Map | `_MaskMap0` or `_MaskMap` | R → metallic map `map_Pm`; A → roughness map `map_Pr` (1 − smoothness) | G (ambient occlusion) and B (detail mask) aren't exported. Metallic Remapping and Smoothness Remapping are ignored. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlTextureMapWriter.cs:122-153 --> |
| Metallic, Smoothness (no Mask Map) | `_Metallic`, `_Smoothness` or `_Glossiness` | Values `Pm` and `Pr` (1 − smoothness) | |
| Emissive Color, Emission Map | `_EmissiveColor` (or `_EmissionColor` with the `_EMISSION` keyword), `_EmissiveColorMap` or `_EmissionMap` | Emission color `Ke` and map `map_Ke` | Exported only when the color isn't black. A black color exports no emission, even with a map. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlTextureMapWriter.cs:155-200 --> |
| Tiling, Offset | Scale and offset of each exported texture property | `-s` and `-o` options of that map | |
| Texture type | Every texture property in this table | Only Texture2D | Another type, such as a cubemap or a Render Texture, is skipped with `[ObjExporter] Material '<m>' property '<p>' holds a <T>, only Texture2D can be exported. Skipped.` <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlTextureMapWriter.cs:378-395 --> |

Shader Graph and Built-in Standard materials export whatever they expose under the property names in the table: the reference names, not the display names. A Standard material's `_Color`, `_BumpMap` and `_MetallicGlossMap` aren't read. See [Set up materials](scene-setup.md#materials).

<a name="menus"></a>
## Menus and assets the project adds

| Menu item | What it does | Who needs it |
| --- | --- | --- |
| **Tools** > **MapBuilder** | Opens the **MapBuilder** window. If it is already open, it reloads the list of publications. | Every map author <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:133-144 --> |
| **Tools** > **CarX Modding** > **Create publisher configs** | Creates any missing vendor settings asset in `Assets/Resources/CarX.Modding`. The project runs the same check whenever the editor loads, and both assets already ship with it. | Integrators <!-- src: Assets/Editor/ModPublisherConfigSetup.cs:18,30-41 --> |
| **Tools** > **mod.io** > **Open Cache** | Opens the data folder of the mod.io plugin in the file browser. | Not needed for map work |
| **Tools** > **mod.io** > **Edit Settings** | Selects the settings asset of the mod.io plugin, and creates it as `Assets/Resources/mod.io/v3_config.asset` if it is missing. MapBuilder doesn't use that asset; it connects with the settings from `ModIoConfig.asset`. | Not needed for map work |
| **ModSystem** > **Sync Publishing Defines** | Recalculates the scripting defines of the publishing layer: `CARX_MODDING_STEAM`, `CARX_MODDING_MODIO` and `CARX_MODDING_MODIO_UPLOAD_PROGRESS`. | Integrators |
| **ModSystem** > **Render Core** > **Regenerate Table**, **Update Lock From Project Render Core**, **Check** | Maintain the render core mirror behind the Material marker. **Update Lock From Project Render Core** is greyed out in this project, because it needs the game's render core package. | Integrators <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RenderCoreMirrorUnity/RenderCoreMirrorMenu.cs:19-22 --> |
| **Map** > **Add vertex animation test to TestMap** | Creates `Assets/MapResources/TestMap/AnimationTest/` with an animated test flag, adds three flags to the `TestMap` sample scene and saves the scene. | Tool developers |
| **Map** > **Enable physics on test flags**, **Map** > **Upgrade test flags to PBR cloth** | Change the test flag that **Add vertex animation test to TestMap** creates. **Upgrade test flags to PBR cloth** needs texture files that don't ship with the project. | Tool developers |

> [!WARNING]
> **Map** > **Add vertex animation test to TestMap** saves its changes into `Assets/MapResources/TestMap/TestMap.unity`. Don't run it if you want to keep the sample scene unchanged. <!-- src: Assets/Editor/VertexAnimationTestScene.cs:97-98 -->

The project also adds these entries to **Assets** > **Create**:

| Menu item | Creates | Notes |
| --- | --- | --- |
| **Assets** > **Create** > **Map** > **GameMarkerTemplateConfig** | A config of road templates | A ready one ships as `Assets/Resources/GameMarkerTemplateConfig.asset`. See [Reuse road values with templates](markers.md#templates). <!-- src: Assets/Scripts/GameMarkerTemplateConfig.cs:4 --> |
| **Assets** > **Create** > **Map** > **MapMetaConfig** | A map config | Use **+ New map** in MapBuilder instead: it also fills in **Name** and selects the map. See [Create or select a map](map-settings.md#new-map). |
| **Assets** > **Create** > **Map** > **MapManagerConfig** | The MapBuilder state asset | Don't create a second one. A second copy in any `Resources` folder makes MapBuilder fail with `(Singleton) Multiple config for : MapManagerConfig`. <!-- src: Assets/Scripts/SingletonScriptableObject.cs:16-28 --> |
| **Assets** > **Create** > **Map** > **MapSkipComponentConfig** | The list of silenced component notes | Don't create a second one; edit the shipped asset. See [MapSkipComponentConfig](#skip-components). |
| **Assets** > **Create** > **CarX** > **Modding** > **mod.io Config**, **Steam Workshop Config** | Vendor settings assets | Both already ship in `Assets/Resources/CarX.Modding`. See [Publishing vendors](advanced/publishing-vendors.md). |
| **Assets** > **Create** > **Modio** > **v3** > **config**, **Modio** > **UI** > **ThemeSheet** | Assets of the mod.io plugin | Not needed for map work. |

<a name="skip-components"></a>
## MapSkipComponentConfig

`Assets/Resources/MapSkipComponentConfig.asset` lists component type names that validation doesn't report as unsupported. It ships with one entry, `DrawZoneBehaviour`: the gizmo helper on the `SpawnPoint` and `Ambient` example prefabs and on the spawn point and minimap of `TestMap`. <!-- src: Assets/Resources/MapSkipComponentConfig.asset:15-16 -->

- An entry only silences the Components note `'<Type>' on '<object>' is not a supported component and will not be exported. …` The component is still not exported. <!-- src: Assets/Editor/Validation/SceneValidator.cs:169-177 -->
- An entry doesn't silence the Format note of preview components, the note about an Animator or SkinnedMeshRenderer without an Animation marker, or the note about a missing script.
- An entry is a class name without its namespace, for example `AudioSource`. It must match exactly, including case.
- The list applies to every map in the project.
- Edit the shipped asset rather than creating another one. With two MapSkipComponentConfig assets in `Resources` folders, validation stops with `(Singleton) Multiple config for : MapSkipComponentConfig` in the Console. <!-- src: Assets/Scripts/SingletonScriptableObject.cs:24-27 -->

To add an entry, see [Hide notes for a component type](validation.md#skip-types).

<a name="window-states"></a>
## MapBuilder window states

The texts in this section are those of the English interface. The Russian version of this page lists the Russian texts. To find out why a control is greyed out, see [Greyed-out controls](troubleshooting.md#greyed-out).

### Header

| Account text | Meaning |
| --- | --- |
| `Account: <user>` | You are signed in to mod.io. The account button reads **Sign out**. <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:537-548 --> |
| `Signing in…` | The sign-in dialogs are open. The button is greyed out. |
| `Not signed in` | You can validate, build and export. To publish, select **Sign in**. See [Sign in to mod.io](publish.md#sign-in). |
| `Unavailable` | mod.io isn't initialized yet or couldn't start. The **Publish** tab shows the reason in an info box. |

The **Platform** field in the header always shows `mod.io` and is greyed out. The language dropdown and the account area are greyed out while a build or an upload runs.

### No map selected

| Where | Text |
| --- | --- |
| Map title | `Select a map` <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:176 --> |
| Subtitle | `Create a map or select an existing map config.` |
| **Build** and **Publish** tabs | `Select a local map or add a new one.` |
| Footer | `Add a map or select an existing map config.` |
| `LOCAL MAPS` list, when the project has no map config or none matches the search | `No maps found.` and `Add a map or change your search.` |

### Rows in the LOCAL MAPS list

Each row shows a build status and a mod.io line. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:179-214 -->

| Text | Meaning |
| --- | --- |
| `Ready to export` | Map and Meta are built, the build cache is on disk, and neither the **Map** tab nor the optimization settings changed since the build. |
| `Not built` | Neither part is built: the map has never been built, or the last build of both parts failed. |
| `Rebuild needed` | Any other case: only one part is built, a setting changed, a publication was created after the build, or the build cache was deleted. |
| `mod.io: not linked` | No publication is linked to this map. |
| `mod.io: version not loaded` | A publication is linked, but you are signed out, or the list doesn't contain it, for example because it was deleted on mod.io. |
| `mod.io: no active file` | The linked publication has no uploaded file. |
| `mod.io: <version>` | The version of the linked publication's current file. |
| `mod.io: version not set` | The current file of the linked publication has no version. |

### Build tab

| Text | Meaning |
| --- | --- |
| `Map geometry and metadata have not been built yet.`, `Map geometry has not been built yet.` or `Map metadata has not been built yet.` | One or both parts are missing. <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:824-833 --> |
| `The build is outdated or its files are missing. Build the selected scene in the current format.` | Both parts were built, but **Scene**, **Format**, the **Map** tab or the optimization settings changed, or the build cache is gone. |
| `<dd.MM.yyyy HH:mm> · Built in <n> s · Resource cache: <n> MB` | The last build succeeded. `Resource cache` is the size of the whole build cache folder, not the size of the mod. The date appears once the window is reopened. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:384-389 --> |
| `Build incomplete. Check validation results and the Console.` | After the last build, Map or Meta is still missing: validation found errors, or the build failed or was cancelled. |
| `Build the map to view its scene contents.` | **Scene contents** has no data, because no build has included **Map** yet. |
| `Before exporting, build the map and its metadata in the Build tab.` | Hint on the **Build** and **Publish** tabs: the build isn't current for the selected **Scene** and **Format**. |
| `Choose an export folder.` | Hint: the build is current and the **Export to folder** option is selected under **Build destination**, but **Folder** is empty. |

### Publish tab

| Text | Meaning |
| --- | --- |
| `Not signed in to https://mod.io/g/carx-dro2.` | You are signed out. Export to a folder still works. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoAuthProvider.cs:154 --> |
| `Build a map to create a publication. To update one, select an existing publication below.` | The **Platform** option under **Build destination** is selected, and no linked publication is selected. |
| `Select a local map.` | Hint for **Create publication**: no map is selected. |
| `Build the map and its metadata first. A publication is created together with its files.` | Hint for **Create publication**: Map or Meta isn't built. |
| `No publications on mod.io yet.` and `Build the map and click Create publication.` | The list is empty. It also shows while you are signed out, because nothing is loaded then. |

For the texts on each publication row, such as `Blank <n>`, `<MB> / 4120 mb`, `Old version!` and `Select for this map`, see **The Publications list** in [Publish to mod.io](publish.md).

### Footer

After an operation, the footer of the window shows one of these texts. <!-- src: Assets/Editor/MapBuilderEditorWindow.Publish.cs:101-105,139-141,182-184 -->

| Text | After |
| --- | --- |
| `Local map: <name>` | Selecting a map |
| `Validation finished. See the map validation window for results.` | **Validate map** |
| `Built in <n> s · Resource cache: <n> MB` or `Build incomplete. Check validation results and the Console.` | The **Build map** button |
| `Exported to <path>` or `Nothing to export — see the Console.` | The **Export to folder** button |
| `Publication created.` or `Publication did not complete. See the Console.` | **Create publication** |
| `Publication updated.` or `Update did not complete. See the Console.` | **Update publication** |

<a name="storage"></a>
## Where settings and builds are stored

| What | Where | Moves with the project folder |
| --- | --- | --- |
| **Map** tab fields, map id, optimization settings | The map config (a MapMetaConfig asset) where you saved it, for example in `Assets/MapResources/<MapName>/` | Yes |
| Selected map, publication links, build records, changelogs, the last **Scene**, and the **Update title**, **Update description** and **Update preview** toggles | `Assets/Resources/MapManagerConfig.asset` | Yes <!-- src: Assets/Editor/MapManagerConfig.cs:24-32 --> |
| Road templates | `Assets/Resources/GameMarkerTemplateConfig.asset`, or a config you created | Yes |
| Silenced component notes | `Assets/Resources/MapSkipComponentConfig.asset` | Yes |
| mod.io connection and limits | `Assets/Resources/CarX.Modding/ModIoConfig.asset` | Yes |
| Build cache | `<temp>/DefaultCompany/ModeMap/StandaloneTemporary<map id>/MapTemp` and `/MetaTemp`. On Windows, `<temp>` is usually `%USERPROFILE%\AppData\Local\Temp`. | No: build again on the other computer <!-- src: Assets/Editor/MapBuilder.Storage.cs:11-14,64-83 --> |
| Interface language | EditorPrefs key `CarX.MapBuilder.Language`: `en` (default) or `ru` | No <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:14,58 --> |
| Last build summary of a map | EditorPrefs key `CarX.MapBuilder.BuildSummary.<map id>` | No |
| **Binary textures** | EditorPrefs key `CarX.Modding.BinaryTextureEncoding`: BC7 by default; applies to every map | No <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/BinaryMapPacker.cs:14-20 --> |
| mod.io email and accepted terms | EditorPrefs keys `CarX.Modding.Publisher.Email.modio` and `CarX.Modding.Publisher.Terms.modio` | No <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/Publishing/ModAuthPreferences.cs:16-17 --> |
| mod.io sign-in token | On disk, saved by the mod.io plugin | No: sign in again |
| **Build destination** and **Folder** | The MapBuilder window itself; closing the window resets them | No |

EditorPrefs belong to your user account on this computer and are shared by all Unity projects. The map id is the GUID of the map config asset; MapUploader sets it automatically.

<a name="glossary"></a>
## Glossary

Use these terms in both languages. The **Meaning** column says where each term is explained.

| EN | RU | Meaning |
| --- | --- | --- |
| Animation marker | маркер Animation | A marker that bakes an Animator's clips into vertex animation. See [Animate objects with vertex animation](markers.md#animation). |
| budget | лимит компонентов | The allowed count of one component type in a map. See [Supported components and budgets](#budgets). |
| build | сборка / собрать | Turning the scene and the map settings into files in the build cache with the **Build map** button. See [Build the map](build.md). |
| build cache | кэш сборки | The folder `StandaloneTemporary<map id>`, with `MapTemp` and `MetaTemp`, where a build is written. It isn't a game package. See [Where the build goes](build.md#cache). |
| changelog | список изменений | The text sent with each uploaded file, entered in **Changelog**. See [Update a publication](publish.md#update). |
| collider | коллайдер | A Mesh, Box, Sphere or Capsule Collider that gives an object collision. See [Add colliders](scene-setup.md#colliders). |
| dynamic object (Rigidbody) | динамический объект (Rigidbody) | An object with a Rigidbody that the game simulates. See [Add dynamic objects](scene-setup.md#dynamic). |
| error / warning / note | ошибка / предупреждение / заметка | The three severities of validation. Only errors stop the build. See [Severities](validation.md#severities). |
| export | экспорт | Copying a current build into a folder with the **Export to folder** button. See [Export the map to the game folder](test-in-game.md#export). |
| format (Binary, Wavefront) | формат (Binary, Wavefront) | How a build is stored: Binary packs it into one `mod.cxmod` file, Wavefront keeps loose files. See [Choose the scene and format](build.md#format). |
| game folder | папка игры | The folder that contains the game's executable on PC. Local copies of maps go into its `Mods` subfolder. See [Where the game looks for local maps](test-in-game.md#local-maps). |
| `Garbage` tag | тег `Garbage` | A tag that leaves an object and its children out of validation, export and optimization. See [Organize the scene](scene-setup.md#garbage). |
| icon | иконка | The **Icon · 16:9** image: the mod.io logo and the image of the map in the game. See [Image requirements](minimap-and-images.md#images). |
| linked publication | связанная публикация | A publication tied to one local map. You link it by selecting its row in **Publications**. See [Update a publication](publish.md#update). |
| local copy | локальная копия | A map exported into `<game folder>/Mods/<MapName>/` for testing. See [Test the map in the game](test-in-game.md). |
| local map | локальная карта | A map in the `LOCAL MAPS` list of MapBuilder, that is, a map config in this project. |
| LOD group | группа LOD | A LODGroup component with up to 8 levels of detail. See [Use LOD groups](scene-setup.md#lod). |
| map | карта | A race track made in this project: a scene and its map config. |
| Map and Meta targets | цели Map и Meta | The two parts of a build. Map holds geometry, collision, markers, lights, dynamic objects and animation; Meta holds the map settings, the icon, the preview and the minimap. |
| map config | настройки карты | The MapMetaConfig asset with the **Map** tab fields, the optimization settings and the map id. **+ New map** creates it. See [Create or select a map](map-settings.md#new-map). |
| map id | ID карты | The GUID of the map config asset. It names the build cache, and a build carries it in its metadata until you link a publication and build again. |
| marker | маркер | A GameMarkerData component with a type: SpawnPoint, Road, Animation or Material. See [Add markers](markers.md). |
| Material marker | маркер Material | A marker that replaces an object's materials with render core variants in Binary builds. See [Replace materials with render core variants](markers.md#material). |
| metadata | метаданные | The Meta part of a build. See *Map and Meta targets*. |
| minimap | мини-карта | The top-down image of the map from the Minimap component. See [Add the minimap](minimap-and-images.md#minimap). |
| mod | мод | Content that the game loads from a mod folder or from mod.io. A map is one kind of mod. |
| mod ID (the mod folder name) | ID мода (имя папки мода) | How the game tells mods apart: a local mod by its folder name, a mod.io install as `<modIoId>_<modfileId>`. |
| `mod.cxmod` | `mod.cxmod` | The single file that a Binary build is packed into on export or upload. |
| optimization (build optimization) | оптимизация сборки | The build stage that cuts static geometry into sectors, merges it and adds LODs. See [Optimization](build.md#optimization). |
| preview | превью | The **Preview · 16:9** image, a large picture stored in the map metadata. See [Image requirements](minimap-and-images.md#images). |
| publication | публикация | A mod entry on mod.io, listed in **Publications**. See [Publish to mod.io](publish.md). |
| render core variant | вариант render core | One of the game's material types that a Material marker can assign. |
| Road marker | маркер Road | A marker that gives an object one of 26 road surfaces. See [Road surfaces and physics fields](#surfaces). |
| sector | сектор | A grid cell that optimization cuts static geometry into. |
| spawn point / fast-travel point | точка появления / точка быстрого перемещения | A SpawnPoint marker. By default, the car starts at the first one, and every one is a fast-travel point on the in-game world map. See [Add spawn points](markers.md#spawn). |
| surface | поверхность | One of the 26 Road presets, such as `Road/asphalt`. Not the same as the **Surface Type** field. |
| template | шаблон | A named set of road physics values in a GameMarkerTemplateConfig. See [Reuse road values with templates](markers.md#templates). |
| validation | проверка | Checking the map against every rule, with **Validate map** or as the first step of a build. See [Fix validation problems](validation.md). |
| vertex animation | вершинная анимация | Animation baked from an Animator's clips, which the game plays without the Animator. See [Animate objects with vertex animation](markers.md#animation). |

## Next steps

For the file format of a build and for adding publishing vendors, see [Mod format: Wavefront and Binary](advanced/mod-format.md) and [Publishing vendors (for integrators)](advanced/publishing-vendors.md).

← [Troubleshooting](troubleshooting.md) · [Contents](README.md) · [Mod format: Wavefront and Binary](advanced/mod-format.md) →
