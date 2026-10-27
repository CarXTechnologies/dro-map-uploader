# Fix validation problems

**English** · [Русский](../ru/validation.md)

Applies to: MapUploader `feature/2.0/3.0` · Unity 6000.3.19f1 · screenshots 2026-10-26

Validation checks the map settings and the scene against the rules of the game and mod.io without building anything. Run it before every build and fix every error it reports; warnings and notes never stop a build.

## Before you start

- A map selected in the **MapBuilder** window. See [Create or select a map](map-settings.md#new-map).
- The map scene, saved and enabled in the scene list of Unity's build settings, so that it appears in **Scene** on the **Build** tab. Scenes under `Assets/MapResources` are added for you. See [Create the map folder and scene](scene-setup.md#scene).

<a name="run"></a>
## Run validation

**Validate map** checks the map settings and the scene in one pass. It builds nothing, doesn't change the scene and needs no mod.io sign-in, so you can run it as often as you like.

To validate the map:

1. In the **MapBuilder** window, select the **Build** tab.
1. To check the right scene, in **Scene**, select the map scene.
   If **Scene** is greyed out, open **Advanced settings** and add **Map** to **Rebuild targets**.
1. Because some checks apply to only one format, in **Format**, select the format you will build.
1. Select **Validate map**.
   If **Scene** isn't the active scene, MapBuilder opens it first. When open scenes have unsaved changes, Unity asks whether to save them; if you cancel, only the map settings are checked.

Result: the **Map Validation** window opens, even when nothing is found, and the footer of the **MapBuilder** window reads `Validation finished. See the map validation window for results.` <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:967 -->

The Console also gets one summary line. It is a warning when the report has errors or warnings, and it leaves out any severity with a count of 0: <!-- src: Assets/Editor/Validation/MapValidationReport.cs:145-165 -->

```text
Map validation - <map name> (<scene>): <n> errors, <n> warnings, <n> notes. The Map Validation window has the list.
Map validation - <map name> (<scene>): no problems found.
```

**Validate map** is greyed out while a build or a publish operation runs and while publication thumbnails download from mod.io.

### Validation during a build

The **Build map** button runs the same checks before it builds anything:

- It checks the map settings first, while the footer shows `Checking map settings…`. If the settings have an error, the build stops there, and the scene isn't checked. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:404 -->
- Like **Validate map**, it then opens **Scene** if it isn't the active scene, even when **Rebuild targets** doesn't include **Map**. If you cancel Unity's prompt to save the open scenes, the build stops without adding a validation issue, and the summary line on the **Build** tab reads `Build incomplete. Check validation results and the Console.` <!-- src: Assets/Editor/MapBuilder.cs:295 -->
- It then checks the scene, while the footer shows `Validating and collecting scene resources…`. This step runs only when **Rebuild targets** under **Advanced settings** includes **Map**. Any error stops the build. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:405 -->
- Warnings and notes never stop a build.

After a build, the **Map Validation** window opens only when the report has at least one issue, and it has no **Re-run** button; with nothing to report, the window closes. When an error stops the build, the summary line on the **Build** tab reads `Build incomplete. Check validation results and the Console.` <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:387 -->

> [!IMPORTANT]
> A build checks the scene only after the map settings pass, so new errors can appear once you fix the settings. Run **Validate map** to see the settings and scene problems together.

<a name="results"></a>
## Read the results

The **Map Validation** window lists every issue from one run, grouped by category. Its first line names the map, the scene and the format as `<map name>  -  <scene>  (<format>)`. The window and its messages are in English in both interface languages. <!-- src: Assets/Editor/Validation/MapValidationWindow.cs:63 -->

The following screenshot shows the window for the TestMap sample with one added Road marker object, `DemoRoadWithoutCollider`, that has no collider. The other issues come from TestMap itself and are notes, which don't stop a build: one for the preview sun and one for each preview-only component.

<img src="../images/shared/validation-window.png" width="900" alt="Screenshot of the Map Validation window with a Markers error; markers 1–5 on the headline, filters, toolbar, category and Select.">

| # | Element | What it does |
| --- | --- | --- |
| 1 | Result headline | Counts the listed errors, warnings and notes. While there are errors, it ends with `- the build cannot proceed until the errors are fixed.` With no issues, it reads `No problems found. The map is ready to build.` |
| 2 | **Errors (n)**, **Warnings (n)**, **Notes** and the search field | Show or hide the issues of each severity; all three are on in a newly opened window. The search keeps only the issues whose message, category or object path contains the text you enter. While the window stays open, the filters and the search text are kept for the next run. |
| 3 | **Re-run**, **To console**, **Copy** | **Re-run** validates again with the same map and format; it appears only in a window opened by **Validate map**. **To console** logs every issue to the Console as `[Map validation] <category>: <message>`, each linked to its object. **Copy** puts the report on the clipboard as plain text. |
| 4 | Category foldout, `<category>  (<n>)` | Groups the issues of one category. Select the header to collapse or expand the group. A collapsed category stays collapsed in later runs until Unity recompiles scripts. Categories are sorted alphabetically. |
| 5 | **Select** | Selects the object or asset the issue is about and highlights it in the **Hierarchy** or the **Project** window. For a scene object, its tooltip shows the object path. |

To work through the results:

1. In the **Map Validation** window, clear **Warnings (n)** and **Notes**.
   Only the errors stay in the list.
1. In the next error row, select **Select**.
   Unity selects the object and highlights it.
1. Fix the object as the message says.
   For the cause and fix of the common messages, see [Troubleshooting](troubleshooting.md).
1. Repeat steps 2 and 3 for each error row, from top to bottom.
   The list doesn't change until you run validation again.
1. Select **Re-run**.
   The list updates.
1. To review the warnings and notes, select **Warnings (n)** and **Notes**.
   All issues show again.

Result: the headline lists no errors, or reads `No problems found. The map is ready to build.` You can now select the **Build map** button.

Issues that aren't about one object, such as a missing spawn point, have no **Select** button. If the object is no longer in the open scene, **Select** shows `'<object path>' is not in the open scene`. <!-- src: Assets/Editor/Validation/MapValidationWindow.cs:184 -->

**Re-run** repeats the check for the map and format of the first run. After you change **Format**, or when the window was opened by a build, select **Validate map** in the **MapBuilder** window instead. After Unity recompiles scripts, the window shows `Nothing has been validated yet.` until you validate again. <!-- src: Assets/Editor/Validation/MapValidationWindow.cs:49 -->

**Copy** writes a plain-text report that you can paste into an issue or a message. Issues are listed in the order validation found them: map settings first, then the scene. Notes are written as `INFO`, and each line about a scene object ends with its path: <!-- src: Assets/Editor/Validation/MapValidationReport.cs:189-212 -->

```text
Map validation - <map name> (<scene>, <format>)
<n> errors, <n> warnings, <n> notes

INFO [Format] 'Volume' is used only for the SDK preview and is not exported or required.  <- <object path>
ERROR [Markers] '<object>' is marked as Road but has no Collider on it or its children, so its surface type will never be used. Add a Mesh/Box/Sphere/Capsule Collider.  <- <object path>
WARNING [Lighting] The map has <n> Directional Lights. These affect the SDK preview only; the game supplies its own environment lighting.  <- <object path>
```

<a name="severities"></a>
## Severities

Every issue has one of three severities. Only errors stop a build.

| Severity | In the window | Stops the build | What it means |
| --- | --- | --- | --- |
| Error | Error icon; counted in **Errors (n)** | Yes | The map can't be built or won't work in the game. Fix every error. |
| Warning | Warning icon; counted in **Warnings (n)** | No | Something is skipped, or will look or behave wrong in the game. Review each warning. |
| Note | Info icon; shown with **Notes**; `INFO` in copied text | No | Information, for example a component that export skips or that only lights the editor preview. |

Validation scans every object in the selected scene, including inactive objects and disabled components. It skips objects tagged `Garbage` together with their children, and objects that the editor hides or doesn't save with the scene. See [Organize the scene](scene-setup.md#garbage).

When a rule that checks individual objects finds more than 25 of them, the window lists the first 25 and adds one note in the `Summary` category: `<rule>: <n> more of the same, not listed individually.` Here `<rule>` is an internal rule name, such as `unsupported-AudioSource` or `sector-render mesh`. <!-- src: Assets/Editor/Validation/MapValidationReport.cs:89-115 -->

> [!NOTE]
> The counts in the headline and on **Errors (n)** and **Warnings (n)** include only the listed issues, not the ones folded into `Summary`.

<a name="catalogue"></a>
## What is checked

Validation checks the map settings first, then the scene. The window sorts the categories alphabetically, and so does this table.

| Category | What it checks | Severities |
| --- | --- | --- |
| Budget | How many components of each supported type the scene has | Errors |
| Components | Components that export skips: unsupported types, missing scripts, animation without a marker | Notes |
| Format | Components that only light the editor preview, and the build format | Notes, an error |
| Geometry | Meshes, material slots, mesh colliders, LOD groups, build optimization and the sector budget | Warnings, an error |
| Lighting | Lights that don't reach the game | Warnings, a note |
| Markers | GameMarkerData: marker types, spawn points, Road colliders, Animation and Material markers | Errors, warnings, a note |
| Meta | The map settings: the scene name, **Name**, the summary, **Description**, the Icon and the Preview | Errors, warnings, a note |
| Minimap | The Minimap size and textures | Errors |
| Physics | Mesh colliders on moving Rigidbodies | An error |
| Scene | Whether **Validate map** could open the selected scene | A note |
| Summary | Issues beyond the first 25 of one rule | Notes |

The following sections quote every message exactly, in the same order. Text in angle brackets stands for a value from your map, such as `<object>` for a GameObject name.

<details>
<summary><b>Budget</b>: component counts</summary>

The scene must stay within these counts. They include inactive objects, disabled components and lights of every type, and they skip objects tagged `Garbage`. <!-- src: Assets/Editor/MapSceneRules.cs:11-24 -->

| Severity | Message | What to do |
| --- | --- | --- |
| Error | `'<type>': the map has <n>, at least <min> is required.` | Add the missing component. It is usually `'Minimap'` or `'GameMarkerData'`. |
| Error | `'<type>': the map has <n>, at most <max> is allowed.` | Remove components of that type, or tag the objects that the game doesn't need `Garbage`. |

| Component | Minimum | Maximum |
| --- | --- | --- |
| Transform (one per GameObject) | 1 | 20 000 |
| MeshCollider | 0 | 10 000 |
| BoxCollider | 0 | 10 000 |
| SphereCollider | 0 | 1000 |
| CapsuleCollider | 0 | 1000 |
| Rigidbody | 0 | 1000 |
| MeshRenderer | 0 | 10 000 |
| MeshFilter | 0 | 10 000 |
| Light | 0 | 500 |
| HDAdditionalLightData | 0 | 500 |
| LODGroup | 0 | 10 000 |
| GameMarkerData | 1 | 10 000 |
| Minimap | 1 | 1 |

</details>

<details>
<summary><b>Components</b>: components that export skips</summary>

Validation accepts the component types of the Budget table. Animator and SkinnedMeshRenderer are accepted too, but only under the Animator that an Animation marker uses. Validation adds a note for every other component, and export skips it. <!-- src: Assets/Editor/Validation/SceneValidator.cs:136-177 -->

| Severity | Message | What to do |
| --- | --- | --- |
| Note | `'<object>' has a missing script in component slot <i>. This slot is skipped during export.` | Remove the missing script from the object, or leave it: export skips it. |
| Note | `'<type>' on '<object>' requires a GameMarkerData Animation marker on the Animator root. This component is skipped; add the marker to export baked vertex animation.` | `<type>` is Animator or SkinnedMeshRenderer. To export the animation, add an Animation marker to the object with the Animator. See [Animate objects with vertex animation](markers.md#animation). |
| Note | `'<type>' on '<object>' is not a supported component and will not be exported. It is skipped automatically; supported components on this object are still exported.` | No action is needed. Unity Terrain is reported here, as `Terrain` and `TerrainCollider`, and isn't exported: convert it to a mesh with a MeshCollider. To stop the note, tag the object `Garbage` if nothing on it should be exported, or follow [Hide notes for a component type](#skip-types). |

</details>

<details>
<summary><b>Format</b>: preview-only components and the build format</summary>

These issues come from components that only the editor preview uses, and from the selected format. <!-- src: Assets/Editor/Validation/SceneValidator.cs:153-157 -->

| Severity | Message | What to do |
| --- | --- | --- |
| Note | `'<type>' is used only for the SDK preview and is not exported or required.` | No action is needed. `<type>` is Volume, ReflectionProbe or HDAdditionalReflectionData; they light only the editor preview. |
| Error | `Unsupported map format. Select Wavefront or Binary and rebuild Map and Meta.` | In **Format**, select **Binary** or **Wavefront**, then build again. The message appears only when a stored format value is invalid. |

</details>

<details>
<summary><b>Geometry</b>: meshes, LOD groups and the sector budget</summary>

These checks cover meshes, material slots, mesh colliders, LOD groups and build optimization. See [Optimization](build.md#optimization). <!-- src: Assets/Editor/Validation/SceneValidator.cs:762-828,853-883 -->

| Severity | Message | What to do |
| --- | --- | --- |
| Warning | `'<object>' has a MeshRenderer but no MeshFilter, so it has no geometry to export.` | Add a MeshFilter with a mesh, or remove the MeshRenderer. |
| Warning | `'<object>' has a MeshFilter with no mesh assigned.` | Assign a mesh, or remove the MeshFilter. |
| Warning | `'<object>' has an empty material slot; it will render as magenta in game.` | Assign a material to every slot of the MeshRenderer. |
| Warning | `'<object>' has a MeshCollider with no mesh assigned, so it collides with nothing.` | Assign a mesh to the MeshCollider, or remove it. |
| Error | `'<object>' has <n> LOD levels; at most 8 are supported and the whole group is skipped on export. Merge or remove levels.` | Reduce the LODGroup to 8 levels or fewer. |
| Warning | `'<object>' has a LODGroup with no renderers assigned to any level.` | Assign renderers to the LOD levels, or remove the LODGroup. |
| Warning | `Build optimization is off: static meshes and colliders are exported as-is, without sector chunking, collider simplification or LODs. Large maps get more draw calls, heavier physics and longer loading. Enable it in Build > Optimization.` | On the **Build** tab, select **Optimization**, then select **Optimize build**. **Select** in this row selects the map config. |
| Warning | `'<object>' (<mesh>): static <kind>, <n> triangles, world bounds <x> x <y> x <z> m. Exceeds sector budget (<size> m / <n> triangles). Review Build > Optimization; size alone does not prove a performance issue.` | `<kind>` is `collider` or `render mesh`. The mesh is exported whole, because **Optimize build** is off, or because **Partition static colliders** (for a collider) or **Partition static render meshes** (for a render mesh) is off. Turn the option on, or split the mesh in your 3D tool. Drive the map after you build it to check the result. |

A sector warning appears for each static mesh that build optimization can process when the mesh has more triangles than **Max triangles per mesh** or is larger than **Sector size (m)** in any world dimension: by default 8192 triangles and 64 m. The warning appears only for meshes that optimization won't cut into sectors: every such mesh while **Optimize build** is off, MeshColliders while **Partition static colliders** is off, and render meshes while **Partition static render meshes** is off. With the default settings there are no sector warnings, and the TestMap sample gets none. The check doesn't list meshes that the optimizer leaves whole for other reasons, such as transparent or lightmapped renderers and objects with more than one MeshCollider; see [Optimization](build.md#optimization). <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/BuildOptimizationSettings.cs:14-19 -->

A mesh counts as static when its object is active, isn't under a Rigidbody, Animator, Animation or LODGroup, isn't tagged `Garbage`, and has no markers other than Road and Material on it or its parents. The check covers enabled MeshRenderers and enabled, non-trigger, non-convex MeshColliders. Colliders are compared with the same two values, although optimization cuts colliders into larger pieces of up to 128 m and 32 768 triangles. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/BuildOptimizationSettings.cs:10-11 -->

</details>

<details>
<summary><b>Lighting</b>: lights that don't reach the game</summary>

Only Point and Spot lights are exported. See [Set up lighting](scene-setup.md#lighting). <!-- src: Assets/Editor/Validation/SceneValidator.cs:713-760 -->

| Severity | Message | What to do |
| --- | --- | --- |
| Note | `'<object>' is a Directional light. It lights the SDK preview only; the game uses its own sun, so it is not exported.` | No action is needed: a preview sun is expected, and the game lights the map itself. The note appears for every enabled Directional light. To clear it, tag the light `Garbage`; it still lights the editor preview. |
| Warning | `'<object>' is a <type> light. Wavefront/Binary exports Point and Spot lights only, so this one will not reach the mod.` | The warning appears for every enabled light that isn't Point, Spot or Directional, such as an Area light. Change the light to Point or Spot, or tag it `Garbage` if it should light only the editor preview. |
| Warning | `The map has <n> Directional Lights. These affect the SDK preview only; the game supplies its own environment lighting.` | Keep one Directional Light for the preview, or tag the extra ones `Garbage`. Disabled Directional Lights count too. |

</details>

<details>
<summary><b>Markers</b>: GameMarkerData</summary>

These checks cover marker types, spawn points, Road markers and Animation markers. See [Add markers](markers.md). <!-- src: Assets/Editor/Validation/SceneValidator.cs:213-294 -->

| Severity | Message | What to do |
| --- | --- | --- |
| Error | `'<object>' has a GameMarkerData with no type selected.` | In the marker, choose a type, or remove the component. |
| Warning | `'<object>' uses unsupported marker '<head>'. This marker will be skipped during export. Supported types are SpawnPoint, Road, Animation and Material.` | Remove the component, or choose a supported type. The old types `Ambient`, `TimeObjectActivator` and `NetworkObject` are reported here. |
| Error | `Animation marker requires an Animator with animation clips.` | In the Animation marker, assign **Animator**, or put the marker on the object with the Animator. The Animator needs a controller with at least one clip. |
| Error | `'<object>' is marked as Road but has no Collider on it or its children, so its surface type will never be used. Add a Mesh/Box/Sphere/Capsule Collider.` | Add a Mesh, Box, Sphere or Capsule Collider to the same GameObject as the marker. Validation also accepts a collider on a child, but export gives that collider no surface. |
| Error | `The map has no GameMarkerData at all - at minimum it needs a spawn point.` | Add a SpawnPoint marker. A scene with no markers also gets the next error and a Budget error. |
| Error | `The map has no SpawnPoint marker, so there is nowhere for a car to appear. Add an empty object with GameMarkerData set to SpawnPoint.` | Create an empty GameObject, add the GameMarkerData component and choose **SpawnPoint**. |
| Warning | `<n> spawn points are all named '<name>'. Wavefront/Binary exports the object name as the spawn point's identity, so give each one a distinct, meaningful name.` | Rename the spawn point GameObjects. The world map in the game lists each spawn point under its name. |

</details>

<details>
<summary><b>Markers</b>: Material markers</summary>

Material markers replace materials with render core variants, and they apply only to Binary builds. Their issues appear in the Markers category. The last two rows are checked only when **Format** is **Binary**. See [Replace materials with render core variants](markers.md#material). <!-- src: Assets/Editor/Validation/SceneValidator.cs:300-524,592-663 -->

| Severity | Message | What to do |
| --- | --- | --- |
| Warning | `'<object>' has a Material marker, but render core materials are exported only in the Binary format. Wavefront ignores the marker and exports the original materials.` | In **Format**, select **Binary**, or remove the marker. Appears only when **Format** is **Wavefront**. |
| Warning | `'<object>' has a Material marker without materials. Press 'Add renderer materials' or remove the marker.` | In the marker, select **Add renderer materials**, or remove the marker. |
| Warning | `'<object>' has a Material marker, but neither a MeshRenderer nor a LODGroup on the same object, so it changes nothing.` | Move the marker to the object with the MeshRenderer or the LODGroup. |
| Warning | `'<object>' is part of a baked vertex animation. Its Material marker is ignored: animated objects keep their own materials.` | Remove the marker. Animated objects keep their own materials. |
| Error | `'<object>' has a Material marker entry without a source material. Assign it or remove the entry.` | In the entry, assign **Source material**, or select **Remove** on the entry header. |
| Error | `'<object>': material '<material>' has no supported render core variant ('<variant>').` | In the entry, choose a **Render core variant**. |
| Error | `'<object>': material '<material>' uses render core variant <variant>, which is unavailable in mods: <reason>. Choose another variant.` | In the entry, choose another **Render core variant**. |
| Warning | `'<object>': material '<material>' is not used by the renderer, so its render core override is never applied.` | Remove the entry, or assign the material to the renderer. |
| Warning | `'<object>': render core material '<material>' (<variant>) stores values that the variant does not have: <names>. They are not exported. Press 'Export from material' or 'Remove' next to 'Unused values' in the marker.` | In the entry, select **Export from material**, or select **Remove** next to **Unused values**. |
| Warning | `'<object>': render core material '<material>' (<variant>) was exported from material for revision <n>, but this SDK has revision <n> of the variant. The game uses the fallback material until you re-run Export from material on this entry.` | In the entry, select **Export from material**. |
| Warning | `'<object>': render core material '<material>' (<variant>) has no diffuse texture and will render white. Press 'Export from material' or assign the texture.` | In the entry, select **Export from material**, or assign the diffuse texture. |
| Warning | `'<renderer>': mesh '<mesh>' has no vertex colors, but <variant> blends its layers by them (RGB - layer mask, A - albedo tint). It is exported white, so only the fourth layer shows. Paint vertex colors in the mesh.` | Paint vertex colors on the mesh in your 3D tool, then import it again. |
| Warning | `'<renderer>': mesh '<mesh>' mixes <variant> and <source> submeshes. The game keeps the layer blend vertex colors for the whole mesh, so the emission is multiplied by them. Move the emissive part to a separate object without layer blend materials: build optimization merges layer blend objects only with each other.` | Move the emissive part to a separate object. `<source>` is an emissive variant or `emissive material '<material>'`. |
| Note | `'<object>': material '<material>' is double-sided and alpha-tested (vegetation?) and is mapped to <variant>. If it is vegetation, MAT_FOLIAGE_ALPHATEST (with a normal map) or MAT_FOLIAGE_SIMPLE_ALPHATEST (without) gives it foliage lighting; for a fence, grate or cable keep <variant>.` | For vegetation, choose `MAT_FOLIAGE_ALPHATEST` or `MAT_FOLIAGE_SIMPLE_ALPHATEST`. For a fence, grate or cable, keep the variant. Binary only. |
| Warning | `Material '<material>' is mapped to different render core variants by Material markers: <uses>. Objects with this material render differently (for example, one LOD loses foliage lighting). Use one variant for this material in all marker entries.` | Choose the same **Render core variant** for this material in every marker entry. Binary only. |

</details>

<details>
<summary><b>Meta</b>: map settings</summary>

These checks read the **Map** tab and the **Scene** selected on the **Build** tab. See [Fill in the Map tab](map-settings.md#map-tab). <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:16-25 -->

The limits come from the mod.io configuration that ships with the project: **Name** up to 50 characters, the summary up to 250, **Description** up to 50 000, and the Icon file up to 8 MB. The Preview file can be up to 10 MB. <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:27-30 -->

In the image messages, `<image>` is `icon` or `preview`. In the rows about an assigned image, **Select** selects the texture in the **Project** window.

| Severity | Message | What to do |
| --- | --- | --- |
| Error | `No target scene is selected. Pick one in Build Settings.` | On the **Build** tab, in **Scene**, select the map scene. If the list is empty, save the scene and enable it in the build scene list. |
| Error | `The scene name '<scene>' cannot be used for a build. Start with a letter and use only letters, digits and underscores.` | Rename the scene file, for example to `MyMap_01.unity`. |
| Error | `The track has no name. Fill in Workshop Name on the map config.` | On the **Map** tab, enter **Name**. The message calls this field Workshop Name. |
| Error | `The track name '<name>' contains characters that cannot be published. Use letters, digits, spaces and -_'.,:!?()&+/, with no leading or trailing spaces.` | Remove other characters, and leading or trailing spaces, from **Name**. |
| Error | `The track name is <n> characters; this vendor allows <limit>.` | Shorten **Name**. |
| Error | `The description is <n> characters; this vendor allows <limit>.` | Shorten **Description**. |
| Error | `This vendor requires a summary and there is nothing to build one from. Fill in Summary, or the first line of the description.` | Enter **Name**. This error appears only when **Name**, **Summary (optional)** and **Description** are all empty, because an empty summary falls back to the first line of **Description**, then to `<Name> track.` |
| Error | `The summary is <n> characters; this vendor allows <limit>. It is taken from the first line of the description - add a shorter Summary instead.` | Enter a short **Summary (optional)**, or shorten the first line of **Description**. |
| Error | `The summary is <n> characters; this vendor allows <limit>. Shorten the Summary field.` | Shorten the first line of **Summary (optional)**. Only that line is used. |
| Error | `No Icon is assigned. It is the image shown for the map in game and on the mod page, and both vendors need one before an entry can be created.` | On the **Map** tab, assign an image in **Icon · 16:9**. |
| Error | `The icon '<texture>' has Read/Write disabled. Enable it in the texture import settings.` | Select the texture. In the **Inspector**, expand **Advanced**, select **Read/Write**, then select **Apply**. |
| Warning | `No Preview is assigned. The map will have no large image on the mod page or on the loading screen.` | Optional: on the **Map** tab, assign an image in **Preview · 16:9**. |
| Error | `The preview '<texture>' has Read/Write disabled. Enable it in the texture import settings.` | Turn on **Read/Write** for the texture, as for the icon. |
| Warning | `The <image> is a <extension> file. The uploader uploads the source file as is and the documented format is PNG.` | `<extension>` is `.jpg` or `.jpeg`. Save the image as PNG and assign it again. |
| Error | `The <image> is a <extension> file, which is uploaded as is and is not an image a vendor accepts. Save it as PNG.` | Save the image as PNG and assign it again. |
| Error | `The <image> is <size> mb; the limit is <limit> mb.` | Make the file smaller: up to 8 MB for the Icon, 10 MB for the Preview. <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:14 --> |
| Note | `The scene itself was not validated - fix the configuration first, then build again.` | Fix the errors in this category. The note is added whenever the map settings or the build format have errors. A build stops before the scene check, but **Validate map** checks the scene anyway and lists its issues too. |

</details>

<details>
<summary><b>Minimap</b>: size and textures</summary>

The scene needs exactly one Minimap; the Budget category checks the count. Keep the Minimap object active: validation also counts an inactive Minimap, but the build exports only an active one. See [Add the minimap](minimap-and-images.md#minimap). <!-- src: Assets/Editor/Validation/SceneValidator.cs:885-934 -->

| Severity | Message | What to do |
| --- | --- | --- |
| Error | `'<object>': Bound Size is <size>. Set it to the size of the map in world units, or the minimap collapses to a point in game.` | In the Minimap component, set **Bounds Size** to the map's extent in world units. The message calls this field Bound Size. |
| Error | `'<object>': no textures assigned. Add at least one entry with a Main Texture.` | Under **Textures**, add an element and assign its **Main Texture**. |
| Error | `'<object>': Textures element <i> has no Main Texture assigned.` | Assign **Main Texture** in that element, or remove the element. |
| Error | `'<object>': Textures element <i> is a <type>. Only Texture2D can be exported.` | Assign an imported image (Texture2D), not a Render Texture or another texture type. |

</details>

<details>
<summary><b>Physics</b>: mesh colliders on moving Rigidbodies</summary>

A Rigidbody that isn't kinematic needs convex mesh colliders. The Rigidbody can be on the collider's object or on a parent. <!-- src: Assets/Editor/Validation/SceneValidator.cs:830-851 -->

| Severity | Message | What to do |
| --- | --- | --- |
| Error | `'<object>' has a non-convex MeshCollider driven by a non-kinematic Rigidbody, which the physics engine no longer supports. Tick Convex, or make the Rigidbody kinematic.` | On the MeshCollider, select **Convex**, or on the Rigidbody, select **Is Kinematic**. |

</details>

<details>
<summary><b>Scene</b> and <b>Summary</b></summary>

The Scene note comes from **Validate map** itself, and the Summary note from the limit of 25 listed issues per rule. <!-- src: Assets/Editor/MapBuilder.cs:389-390 -->

| Severity | Message | What to do |
| --- | --- | --- |
| Note | `'<scene>' was not opened, so only the configuration was checked.` | Appears when you cancel the prompt to save the open scenes, or when no scene is selected; `<scene>` is then empty. Select **Validate map** again, and save or discard the changes when Unity asks. |
| Note | `<rule>: <n> more of the same, not listed individually.` | Fix the listed issues of that rule, then validate again to see the rest. |

</details>

<a name="skip-types"></a>
### Hide notes for a component type

Unsupported components are notes and never stop a build. If a component type is in the scene on purpose, for example sound sources that you keep for reference, you can stop validation from listing it.

To hide the notes for a component type:

1. In the **Project** window, select `Assets/Resources/MapSkipComponentConfig.asset`. <!-- src: Assets/Resources/MapSkipComponentConfig.asset:15-16 -->
1. In the **Inspector**, in the **Valid** list, select **+**.
1. In the new element, enter the component's type name exactly as the note shows it, for example `AudioSource`.

Result: the next validation no longer lists `'AudioSource' on '<object>' is not a supported component…` notes. The component is still not exported.

- The list applies to every map in the project. It ships with one entry, `DrawZoneBehaviour`, the gizmo helper on the example spawn point and minimap objects.
- An entry hides only the "not a supported component" note. The Format notes for preview components, the notes for an Animator without an Animation marker, and missing scripts are still listed.
- Edit this asset rather than creating another one. With two MapSkipComponentConfig assets in `Resources` folders, validation stops with `(Singleton) Multiple config for : MapSkipComponentConfig` in the Console. <!-- src: Assets/Scripts/SingletonScriptableObject.cs:26 -->

<a name="elsewhere"></a>
## Checked elsewhere

Some problems never appear in the **Map Validation** window. Check for them separately.

**Build size.** MapBuilder checks the size of the map and its metadata only immediately before **Create publication** or **Update publication**. With mod.io, the map can be up to 4096 MB and the metadata up to 24 MB. <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:25-26 -->

A build that is too large stops the upload with one of these errors in the Console:

```text
'<scene>': the map is <size> mb and this vendor accepts <limit> mb.
'<scene>': the meta is <size> mb and this vendor accepts <limit> mb.
```

Both sizes are measured on the build cache folders `MapTemp` and `MetaTemp`, not on the packed `mod.cxmod`. **Validate map** and the **Export to folder** button don't check them. See [Build size](reference.md#limits-size). <!-- src: Assets/Editor/MapBuilder.Storage.cs:158-182 -->

**Export errors.** Some problems are found only while the build exports the scene. They stop the build and appear in the Console as `InvalidOperationException`, and the summary line on the **Build** tab reads `Build incomplete. Check validation results and the Console.` For example: <!-- src: Assets/Editor/MapBuilder.cs:323-327 -->

```text
Rigidbody '<name>' cannot split an enclosing LODGroup. Put the Rigidbody on the LODGroup root.
Rigidbody '<name>': collider '<object>' has a sheared transform. Apply scale in the model first.
Put Rigidbody on the Animator root or above it; a baked animation cannot contain independent moving bodies.
Animator '<name>' has multiple Animation markers.
VAT for '<name>' is too large (<width>x<height>). Reduce vertices, clips or sample rate.
meshoptimizer is unavailable for this Editor platform. Install its native Editor library or disable build optimization.
```

Export also logs warnings that don't stop the build, for example `Rigidbody '<name>' was skipped: no enabled supported colliders. Geometry and animation will still be exported.` For the causes and fixes, see [Troubleshooting](troubleshooting.md). <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:94 -->

**Image size in pixels.** Validation checks the format, Read/Write and file size of the Icon and the Preview, but not their size in pixels or their aspect ratio. mod.io needs an Icon of at least 512 × 288. See [Image requirements](minimap-and-images.md#images). <!-- src: Assets/Plugins/Modio/Modio/API/Generated/Definitions/RequestObjects/EditModRequest.cs:31 -->

## Next steps

When **Validate map** shows no errors, select the **Build map** button, then export the map and drive it in the game.

← [Build the map](build.md) · [Contents](README.md) · [Test the map in the game](test-in-game.md) →
