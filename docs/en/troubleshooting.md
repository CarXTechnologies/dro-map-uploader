# Troubleshooting

**English** · [Русский](../ru/troubleshooting.md)

Run **Validate map**, fix the errors from the top, then build again.

Find the message or the symptom you see, and apply its fix. This page describes only the common validation messages; for every message, see [What is checked](validation.md#catalogue). Messages are quoted in English, exactly as the tools show them. Text in angle brackets stands for a value from your map, such as `<object>` for a GameObject name.

The page has these sections:

- [Validation messages](#validation-messages)
- [Build errors in the Console](#build-errors)
- [Greyed-out controls](#greyed-out)
- [Build state](#build-state)
- [Problems in the game](#game-problems)
- [Upload problems](#upload-problems)
- [Setup problems](#setup-problems)
- [Common questions](#faq)

<a name="validation-messages"></a>
## Validation messages

The **Map Validation** window lists these messages after you select **Validate map** or the **Build map** button. Errors block the build; warnings and notes don't. Most messages have a **Select** button that selects the object or asset the message is about. The entries are grouped by category, in the order of the window. To read the window and use its filters, see [Read the results](validation.md#results).

### Budget

<a name="msg-budget-count"></a>
#### `'Minimap': the map has 0, at least 1 is required.`

Severity: Error (blocks the build) · Category: Budget <!-- src: Assets/Editor/Validation/SceneValidator.cs:198-199 -->

**Cause.** The scene has no Minimap component outside `Garbage`. With two or more, the message is `'Minimap': the map has <n>, at most 1 is allowed.` The same two Budget messages appear for every component type whose count is outside its limit, for example too many Transforms or Rigidbodies.

**Fix.**

- Keep exactly one Minimap component in the scene, on an active object outside any `Garbage` subtree. An inactive Minimap passes validation, but it isn't exported.

See also: [Add the minimap](minimap-and-images.md#minimap) · [Supported components and budgets](reference.md#budgets)

### Components

<a name="msg-animator-no-marker"></a>
#### `'<type>' on '<object>' requires a GameMarkerData Animation marker on…`

Message: `'<type>' on '<object>' requires a GameMarkerData Animation marker on the Animator root. This component is skipped; add the marker to export baked vertex animation.`

Severity: Note (doesn't block the build) · Category: Components <!-- src: Assets/Editor/Validation/SceneValidator.cs:163-165 -->

**Cause.** An Animator or a SkinnedMeshRenderer isn't inside a hierarchy that an Animation marker bakes. Export skips the component: the object doesn't move in the game, and a mesh that only a SkinnedMeshRenderer draws isn't exported at all.

**Fix.**

1. In the **Hierarchy**, select the GameObject with the Animator at the root of the animated hierarchy.
1. Add the GameMarkerData component and select **Animation** in its type menu.

See also: [Animate objects with vertex animation](markers.md#animation)

<a name="msg-unsupported-component"></a>
#### `'<type>' on '<object>' is not a supported component and will not be…`

Message: `'<type>' on '<object>' is not a supported component and will not be exported. It is skipped automatically; supported components on this object are still exported.`

Severity: Note (doesn't block the build) · Category: Components <!-- src: Assets/Editor/Validation/SceneValidator.cs:174-176 -->

**Cause.** The object carries a component that a map can't store, for example Terrain, TerrainCollider, AudioSource, a Camera or a particle system. Export skips that component and keeps the supported components on the same object.

**Fix.** Nothing is required unless the skipped component matters for driving:

- For Terrain, replace the terrain with a mesh that has a MeshCollider. Unity Terrain isn't exported, so the car falls through it in the game.
- To hide the note for a component type that you keep on purpose, follow [Hide notes for a component type](validation.md#skip-types).
- To leave a whole helper object out of validation and export, set its **Tag** to `Garbage`.

See also: [Unity Terrain isn't exported](scene-setup.md#before-you-start) · [MapSkipComponentConfig](reference.md#skip-components) · [Organize the scene](scene-setup.md#garbage)

### Geometry

<a name="msg-empty-material-slot"></a>
#### `'<object>' has an empty material slot; it will render as magenta in game.`

Severity: Warning (doesn't block the build) · Category: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:800-801 -->

**Cause.** An element of **Materials** on a MeshRenderer has no material assigned.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, on the MeshRenderer, assign a material to every element of **Materials**.

See also: [Set up materials](scene-setup.md#materials)

<a name="msg-lod-levels"></a>
#### `'<object>' has <n> LOD levels; at most 8 are supported and the whole…`

Message: `'<object>' has <n> LOD levels; at most 8 are supported and the whole group is skipped on export. Merge or remove levels.`

Severity: Error (blocks the build) · Category: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:829-832 -->

**Cause.** A LODGroup has more than 8 levels. Export would skip the whole group.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, on the LODGroup, remove levels until 8 or fewer remain.

See also: [LOD groups](scene-setup.md#lod)

<a name="msg-optimization-off"></a>
#### `Build optimization is off: static meshes and colliders are exported…`

Message: `Build optimization is off: static meshes and colliders are exported as-is, without sector chunking, collider simplification or LODs. Large maps get more draw calls, heavier physics and longer loading. Enable it in Build > Optimization.`

Severity: Warning (doesn't block the build) · Category: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:874-876 -->

**Cause.** **Optimize build** is turned off for this map. Static meshes and colliders are exported as they are, without sectors or LODs, so a large map draws and loads more slowly in the game.

**Fix.**

1. In the **MapBuilder** window, on the **Build** tab, select **Optimization**.
1. Select **Optimize build**.
1. Select the **Build map** button.

On macOS and Linux, leave **Optimize build** off: its native library ships only for the Windows x64 Editor.

See also: [Optimization](build.md#optimization)

<a name="msg-sector-budget"></a>
#### `'<object>' (<mesh>): static <kind>, <n> triangles, world bounds…`

Message: `'<object>' (<mesh>): static <kind>, <n> triangles, world bounds <x> x <y> x <z> m. Exceeds sector budget (<size> m / <n> triangles). Review Build > Optimization; size alone does not prove a performance issue.`

Severity: Warning (doesn't block the build) · Category: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:890-892 -->

**Cause.** A static render mesh, or an enabled MeshCollider that isn't a trigger or convex, is larger than **Sector size (m)** or has more triangles than **Max triangles per mesh**, and the build exports it whole: **Optimize build** is off, or the Partition option for this kind of mesh is off. With the default settings, this warning doesn't appear. For which meshes the check covers, see [Sector budget](reference.md#sector-budget). <!-- src: Assets/Editor/Validation/SceneValidator.cs:895 -->

**Fix.** On the **Build** tab, under **Optimization**, turn on **Optimize build** and the Partition option for this kind of mesh, then build the map and drive it to check the result. If you keep the option off, split the mesh in your 3D tool.

See also: [Optimization](build.md#optimization)

### Lighting

<a name="msg-directional-light"></a>
#### `'<object>' is a Directional light. It lights the SDK preview only; the…`

Message: `'<object>' is a Directional light. It lights the SDK preview only; the game uses its own sun, so it is not exported.`

Severity: Note (doesn't block the build) · Category: Lighting <!-- src: Assets/Editor/Validation/SceneValidator.cs:755-758 -->

**Cause.** The scene has an enabled Directional light, usually a preview sun such as `sunlight` from an environment preset. It lights only the scene in the editor, and the game supplies its own sun and sky. With two or more Directional lights, enabled or not, the window also shows the warning `The map has <n> Directional Lights. These affect the SDK preview only; the game supplies its own environment lighting.`

**Fix.** No fix is needed for preview lighting, such as an environment preset from `Assets/Art/Internal/EnvironmentProfiles/`. To keep the report short, put the preview lighting inside an object whose **Tag** is `Garbage`: validation and export skip that subtree, and the lights still light the scene in the editor.

See also: [Set up lighting](scene-setup.md#lighting)

<a name="msg-light-type"></a>
#### `'<object>' is a <type> light. Wavefront/Binary exports Point and Spot…`

Message: `'<object>' is a <type> light. Wavefront/Binary exports Point and Spot lights only, so this one will not reach the mod.`

Severity: Warning (doesn't block the build) · Category: Lighting <!-- src: Assets/Editor/Validation/SceneValidator.cs:762-765 -->

**Cause.** An enabled light is neither a Point, a Spot nor a Directional light, for example an Area light. Only Point and Spot lights are exported, so this light lights only the scene in the editor.

**Fix.** To export the light, change its **Type** to **Point** or **Spot**. To keep it for the editor preview only, put it inside an object whose **Tag** is `Garbage`: validation and export skip that subtree, and the light still lights the scene in the editor.

See also: [Set up lighting](scene-setup.md#lighting)

### Markers

<a name="msg-unsupported-marker"></a>
#### `'<object>' uses unsupported marker '<head>'. This marker will be skipped…`

Message: `'<object>' uses unsupported marker '<head>'. This marker will be skipped during export. Supported types are SpawnPoint, Road, Animation and Material.`

Severity: Warning (doesn't block the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:240-242 -->

**Cause.** The GameMarkerData uses a marker type that the game no longer reads: `Ambient`, `TimeObjectActivator` or `NetworkObject`. The **Inspector** shows the same problem as `Unsupported marker: <head>. Select SpawnPoint, Road, Animation or Material, or remove the component.` The example prefab `Assets/ExamplePrefab/Ambient.prefab` and the TestMap sample carry such markers.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in the context menu of the GameMarkerData component, select **Remove Component**.

If the object should carry a marker, select **SpawnPoint**, a **Road** surface, **Animation** or **Material** in the type menu instead.

See also: [Remove unsupported markers](markers.md#unsupported)

<a name="msg-animation-no-animator"></a>
#### `Animation marker requires an Animator with animation clips.`

Severity: Error (blocks the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:255-256 -->

**Cause.** An Animation marker has no Animator: neither in its **Animator** field nor on its own GameObject. Or the Animator has no controller, or its controller has no clips.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in the Animation marker, assign to **Animator** the Animator at the root of the animated hierarchy.
1. On that Animator, assign a controller that has at least one clip.

See also: [Animate objects with vertex animation](markers.md#animation)

<a name="msg-road-unavailable-surface"></a>
#### `'<object>' uses the surface '<surface>', which the game doesn't have…`

Message: `'<object>' uses the surface '<surface>', which the game doesn't have, so its collider gets no surface in the game. Use '<replacement>' instead.`

Severity: Warning (doesn't block the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:264-270,722-727 -->

**Cause.** The Road marker uses `Road/default`, `Road/ice_snowy` or `Road/pavement`. The type menu still offers these surfaces, but the game has no surface template with these names, so the collider gets no road surface, with values or without, and behaves like a collider without a Road marker. In the TestMap sample, `Content/workshop_map_main/wall_coll` uses `Road/default`. <!-- src: game -->

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the type menu of the GameMarkerData component, select the surface that the message names: **Road** > **asphalt** instead of `default`, **Road** > **road_icy** instead of `ice_snowy`, or **Road** > **paving_slab** instead of `pavement`.

If the marker uses **Custom** values, the new surface resets them to its defaults.

See also: [Mark road surfaces](markers.md#road) · [Road surfaces and physics fields](reference.md#surfaces)

<a name="msg-road-no-collider"></a>
#### `'<object>' is marked as Road but has no Collider on it or its children…`

Message: `'<object>' is marked as Road but has no Collider on it or its children, so its surface type will never be used. Add a Mesh/Box/Sphere/Capsule Collider.`

Severity: Error (blocks the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:274-276 -->

**Cause.** A GameMarkerData set to a **Road** surface is on an object that has no collider, neither on the object itself nor on a child. A render mesh alone gives the car nothing to drive on.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, select **Add Component** and add a MeshCollider, BoxCollider, SphereCollider or CapsuleCollider to the same object.

> [!IMPORTANT]
> Keep the Road marker and the collider on the same GameObject. Validation also accepts a collider on a child, but the game gives that child collider no surface.

See also: [Mark road surfaces](markers.md#road)

<a name="msg-no-spawn"></a>
#### `The map has no SpawnPoint marker, so there is nowhere for a car…`

Message: `The map has no SpawnPoint marker, so there is nowhere for a car to appear. Add an empty object with GameMarkerData set to SpawnPoint.`

Severity: Error (blocks the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:289-291 -->

**Cause.** No GameMarkerData component in the scene is set to **SpawnPoint**. Markers inside an object tagged `Garbage` don't count. When the scene has no GameMarkerData at all, the window also shows `The map has no GameMarkerData at all - at minimum it needs a spawn point.` and `'GameMarkerData': the map has 0, at least 1 is required.`

**Fix.**

1. In the **Project** window, open `Assets/ExamplePrefab/` and drag `SpawnPoint.prefab` into the scene.
1. Move the new object to the place where the car should appear.
1. Rename the object. The world map in the game lists every spawn point under its GameObject name.

See also: [Add spawn points](markers.md#spawn)

<a name="msg-render-core-revision"></a>
#### `'<object>': render core material '<material>' (<variant>) was exported…`

Message: `'<object>': render core material '<material>' (<variant>) was exported from material for revision <n>, but this SDK has revision <n> of the variant. The game uses the fallback material until you re-run Export from material on this entry.`

Severity: Warning (doesn't block the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:450-453 -->

**Cause.** A Material marker entry holds values exported for an older revision of its render core variant than the SDK in this project has. Until you export the entry again, the game uses a fallback material instead of the render core variant.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in the Material marker, find the entry for the named material and select **Export from material**.
1. Select the **Build map** button.

See also: [Replace materials with render core variants](markers.md#material)

<a name="msg-render-core-no-diffuse"></a>
#### `'<object>': render core material '<material>' (<variant>) has no diffuse…`

Message: `'<object>': render core material '<material>' (<variant>) has no diffuse texture and will render white. Press 'Export from material' or assign the texture.`

Severity: Warning (doesn't block the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:383-385 -->

**Cause.** A Material marker entry has no diffuse texture, so the game draws the surface white.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in the Material marker, find the entry for the named material and select **Export from material**.
   The entry takes its textures from the source material.

See also: [Replace materials with render core variants](markers.md#material)

### Meta

<a name="msg-scene-name"></a>
#### `The scene name '<scene>' cannot be used for a build. Start with a…`

Message: `The scene name '<scene>' cannot be used for a build. Start with a letter and use only letters, digits and underscores.`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:39-40 -->

**Cause.** The scene file name must start with a letter and use only letters, digits and underscores, for example `MyTrack_01`. Spaces and hyphens aren't allowed. <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:32-45 -->

**Fix.**

1. In the **Project** window, rename the scene file.
1. Open the renamed scene and save it. Saving a scene under `Assets/MapResources` adds it to the build scene list.
1. In the **MapBuilder** window, on the **Build** tab, select the renamed scene in **Scene**.

See also: [Create the map folder and scene](scene-setup.md#scene)

<a name="msg-track-name"></a>
#### `The track name '<name>' contains characters that cannot be published.…`

Message: `The track name '<name>' contains characters that cannot be published. Use letters, digits, spaces and -_'.,:!?()&+/, with no leading or trailing spaces.`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:54-56 -->

**Cause.** **Name** can contain only letters, digits, spaces and `- _ ' . , : ! ? ( ) & + /`, with no space at the start or the end, and at most 50 characters. A longer name gives `The track name is <n> characters; this vendor allows 50.` An empty one gives `The track has no name. Fill in Name on the Map tab.` <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:8; Assets/Resources/CarX.Modding/ModIoConfig.asset:28; Assets/Editor/Validation/MapMetaValidator.cs:48,62 -->

**Fix.**

- In the **MapBuilder** window, on the **Map** tab, edit **Name**.

See also: [Fill in the Map tab](map-settings.md#map-tab)

<a name="msg-summary-length"></a>
#### `The summary is <n> characters; this vendor allows 250.…`

Message: `The summary is <n> characters; this vendor allows 250. Shorten the Summary field.`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:91-95 -->

**Cause.** The summary is longer than 250 characters. MapBuilder uses only the first line of **Summary (optional)**; when that field is empty, it uses the first line of **Description**, and the message ends with `It is taken from the first line of the description - add a shorter Summary instead.` A **Description** over 50 000 characters gives a similar error: `The description is <n> characters; this vendor allows 50000.` <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:29-30; Assets/Editor/Validation/MapMetaValidator.cs:73,94 -->

**Fix.**

- In the **MapBuilder** window, on the **Map** tab, in **Summary (optional)**, enter a single line of at most 250 characters.

See also: [Fill in the Map tab](map-settings.md#map-tab)

<a name="msg-no-icon"></a>
#### `No Icon is assigned. It is the image shown for the map in game and…`

Message: `No Icon is assigned. It is the image shown for the map in game and on the mod page, and both vendors need one before an entry can be created.`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:103-105 -->

**Cause.** The map has no **Icon**. The Icon becomes the mod.io logo and the map's picture in the game, and mod.io can't create a publication without it. A missing **Preview** is only a warning: `No Preview is assigned. The map will have no large image on the loading screen.`

**Fix.**

- In the **MapBuilder** window, on the **Map** tab, assign a PNG to **Icon · 16:9**.

See also: [Image requirements](minimap-and-images.md#images)

<a name="msg-icon-read-write"></a>
#### `The icon '<texture>' has Read/Write disabled. Enable it in the texture…`

Message: `The icon '<texture>' has Read/Write disabled. Enable it in the texture import settings.`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:111-112 -->

**Cause.** The Icon texture is imported with **Read/Write** turned off. The Preview has the same rule and the message `The preview '<texture>' has Read/Write disabled. Enable it in the texture import settings.` A capture that CaptureCamera saved outside `Assets` and that you then copied into the project has this problem: it gets Unity's default import settings, with **Read/Write** off.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
   The texture is selected in the **Project** window.
1. In the **Inspector**, expand **Advanced** and select **Read/Write**.
1. Select **Apply**.

See also: [Set the import settings](minimap-and-images.md#import-settings)

<a name="msg-icon-format"></a>
#### `The <image> is a <extension> file, which is uploaded as is and is not…`

Message: `The <image> is a <extension> file, which is uploaded as is and is not an image a vendor accepts. Save it as PNG.`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:165-167 -->

**Cause.** `<image>` is `icon` or `preview`. The uploader sends the Icon and the Preview to mod.io unchanged, so both must be PNG files. A `.jpg` or `.jpeg` file gives only a warning: `The <image> is a <extension> file. The uploader uploads the source file as is and the documented format is PNG.` A file over the size limit gives `The <image> is <size> mb; the limit is <limit> mb.`

**Fix.**

1. In your image editor, save the image as a PNG that meets the [image requirements](minimap-and-images.md#images).
1. In the **MapBuilder** window, on the **Map** tab, assign the PNG to **Icon · 16:9** or **Preview · 16:9**.

See also: [Image limits](reference.md#limits-images)

<a name="msg-scene-not-validated"></a>
#### `The scene itself was not validated - fix the configuration first…`

Message: `The scene itself was not validated - fix the configuration first, then build again.`

Severity: Note (doesn't block the build) · Category: Meta <!-- src: Assets/Editor/MapBuilder.cs:292-293 -->

**Cause.** The map settings have at least one error. A build stops at the settings and doesn't check the scene, so more errors can appear once you fix them.

**Fix.**

1. Fix the Meta errors in the **Map Validation** window.
1. Select **Validate map** to see the remaining problems in the scene.

See also: [Run validation](validation.md#run)

### Minimap

<a name="msg-bounds-size"></a>
#### `'<object>': Bounds Size is <size>. Set it to the size of the map in…`

Message: `'<object>': Bounds Size is <size>. Set it to the size of the map in world units, or the minimap collapses to a point in game.`

Severity: Error (blocks the build) · Category: Minimap <!-- src: Assets/Editor/Validation/SceneValidator.cs:916-918 -->

**Cause.** The X or the Y value of **Bounds Size** on the Minimap component is zero. Without a size, the minimap collapses to a point in the game.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in **Bounds Size**, enter the world area that the minimap image covers, in metres: **X** is the world X size and **Y** is the world Z size.

To copy the object's world X and Z scale into **Bounds Size** automatically, select **Lock Size**.

See also: [Create the Minimap and frame its area](minimap-and-images.md#frame-area)

<a name="msg-no-textures"></a>
#### `'<object>': no textures assigned. Add at least one entry with a Main…`

Message: `'<object>': no textures assigned. Add at least one entry with a Main Texture.`

Severity: Error (blocks the build) · Category: Minimap <!-- src: Assets/Editor/Validation/SceneValidator.cs:924-925 -->

**Cause.** The **Textures** list of the Minimap component is empty. Two related errors have the same fix: `'<object>': Textures element <i> has no Main Texture assigned.` and `'<object>': Textures element <i> is a <type>. Only Texture2D can be exported.`

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in **Textures**, add an element and assign your minimap image, an imported PNG, to its **Main Texture**. The game shows only element 0.

See also: [Assign the minimap image](minimap-and-images.md#assign-minimap) · [Capture images with CaptureCamera](minimap-and-images.md#capture)

### Physics

<a name="msg-non-convex-dynamic"></a>
#### `'<object>' has a non-convex MeshCollider driven by a non-kinematic…`

Message: `'<object>' has a non-convex MeshCollider driven by a non-kinematic Rigidbody, which the physics engine no longer supports. Tick Convex, or make the Rigidbody kinematic.`

Severity: Error (blocks the build) · Category: Physics <!-- src: Assets/Editor/Validation/SceneValidator.cs:861-863 -->

**Cause.** A MeshCollider with **Convex** cleared is on a Rigidbody's object or inside it, and the Rigidbody has **Is Kinematic** cleared. The game's physics can't move a non-convex mesh collider.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, on the MeshCollider, select **Convex**.

If the object must never move, select **Is Kinematic** on its Rigidbody instead.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

### Scene

<a name="msg-scene-not-opened"></a>
#### `'<scene>' was not opened, so only the configuration was checked.`

Severity: Note (doesn't block the build) · Category: Scene <!-- src: Assets/Editor/MapBuilder.cs:391 -->

**Cause.** **Validate map** had to open the scene selected in **Scene**, and you cancelled the prompt to save the scene that was open. Only the map settings were checked. The note also appears, with an empty name (`'' was not opened, …`), when no scene is selected in **Scene**; see [The Scene dropdown is empty](#scene-empty). <!-- src: Assets/Editor/MapBuilder.cs:391,399-404 -->

**Fix.**

- Select **Validate map** again, and in the prompt, save or discard the changes of the open scene.

See also: [Run validation](validation.md#run)

<a name="build-errors"></a>
## Build errors in the Console

Some problems are found only while the map is exported. They stop the build with an exception in the Console, not in the **Map Validation** window: the Console line starts with the exception type, usually `InvalidOperationException`, or `InvalidDataException` for a Layered Lit material. The **Build** tab then shows `Build incomplete. Check validation results and the Console.` To open the Console, select **Window** > **General** > **Console**. <!-- src: Assets/Editor/MapBuilder.cs:324-328; Assets/Editor/MapBuilderEditorWindow.Workspace.cs:387 -->

<a name="export-errors"></a>
### Export errors

The following table lists every error that stops a build during export, or an export or publication while the map is packed. The entries after the tables describe the common ones in detail.

| Message | Cause | Fix |
| --- | --- | --- |
| `Rigidbody '<name>' cannot split an enclosing LODGroup. Put the Rigidbody on the LODGroup root.` | A Rigidbody is on a child object inside a LODGroup's hierarchy, not on the object with the LODGroup. | Move the Rigidbody to the object with the LODGroup. [Details](#msg-rigidbody-lodgroup) |
| `Rigidbody '<name>': collider '<object>' has a sheared transform. Apply scale in the model first.` | A collider of a dynamic object is rotated inside a parent whose X, Y and Z scale differ. | Give every parent of the collider the same scale on X, Y and Z. [Details](#msg-rigidbody-sheared) |
| `Rigidbody '<name>': MeshCollider has no mesh.` | An enabled MeshCollider of a dynamic object has no **Mesh**. | Assign a mesh to the MeshCollider, or remove the collider. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:75 --> |
| `Rigidbody '<name>': a dynamic MeshCollider must be Convex.` | A Rigidbody with **Is Kinematic** cleared has a MeshCollider with **Convex** cleared. | Select **Convex** on the MeshCollider. [Details](#msg-rigidbody-convex) |
| `Put Rigidbody on the Animator root or above it; a baked animation cannot contain independent moving bodies.` | A Rigidbody is inside the hierarchy of an Animator that an Animation marker bakes. | Move the Rigidbody to the Animator's object or to one of its parents. [Details](#msg-rigidbody-in-animation) |
| `Animation marker '<name>' needs an Animator inside the exported map.` | The marker's Animator, from its **Animator** field or its own object, is tagged `Garbage`, is inside an object tagged `Garbage`, or isn't in the map scene. Validation doesn't check this. | Assign an Animator from the map scene, outside any `Garbage` subtree. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:31-33 --> |
| `Animator '<name>' has multiple Animation markers.` | Two or more Animation markers use the same Animator. | Keep one Animation marker for each Animator. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:34-35 --> |
| `Animation marker hierarchies must not overlap.` or `VAT '<name>': nested Animators must be exported as separate animation objects.` | The hierarchy of an animated Animator contains another Animator, with or without its own Animation marker. | Move the inner animated object out of the outer Animator's hierarchy, or remove the inner Animator. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:36-38,92-93 --> |
| `Invalid animation speed/phase on '<name>'.` | **Playback speed** is negative or not a number, or **Initial phase (0..1)** is outside 0–1. The **Inspector** keeps both in range, so a script or an edited asset set them. | In the Animation marker, set **Playback speed** to 0 or more and **Initial phase (0..1)** to a value from 0 to 1. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:39-41 --> |
| `Animation clip index on '<name>' must be 0..<n>.` | **Initial clip** points to a clip that the Animator's controller no longer has, for example after you removed clips from it. | In the Animation marker, select **Initial clip** again. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:42-44 --> |
| `VAT sample rate must be 1..60.` | **Bake frames per second** is outside 1–60. The **Inspector** slider keeps it in range, so a script or an edited asset set it. | Set **Bake frames per second** to a value from 1 to 60. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:87 --> |
| `Animator '<name>' has no clips.` | The Animator has no controller, or its controller has no clips. Validation reports the same problem as `Animation marker requires an Animator with animation clips.` | Assign a controller with at least one clip. [Details](#msg-animation-no-animator) <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:89 --> |
| `VAT '<name>': use one mesh detail level per animation; nested LODGroups are not supported.` | The hierarchy of an animated Animator contains a LODGroup. | Remove the LODGroup, and keep one mesh for each animated part. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:90-91 --> |
| `Animator '<name>' has no mesh renderers.` | The hierarchy of an animated Animator has no enabled MeshRenderer or SkinnedMeshRenderer on an active object. | Put the meshes that should move inside the Animator's hierarchy. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:109-111 --> |
| `Animation renderer has no mesh.` | A renderer in an animated hierarchy has no mesh or an empty one, for example a MeshRenderer without a MeshFilter. | Assign a mesh, or remove the renderer. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:112-113 --> |
| `VAT for '<name>' is too large (<width>x<height>). Reduce vertices, clips or sample rate.` | The texture of the baked vertex animation would be too large. | Lower **Bake frames per second**, or reduce the clips or the vertices. [Details](#msg-vat-too-large) |
| `Animation mesh material/submesh count differs.` | A renderer in an animated hierarchy has a different number of **Materials** than its mesh has submeshes. | Make the **Materials** list as long as the mesh's submesh count. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:139 --> |
| `VAT supports triangle meshes only.` | A mesh in an animated hierarchy is made of quads, lines or points. | Export the mesh with triangles from your 3D tool, and import it again. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:142 --> |
| `VAT mesh needs vertex normals.` | A mesh in an animated hierarchy has no normals. | In the import settings of the model, set **Normals** to **Import** or **Calculate**, and select **Apply**. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:216 --> |
| `VAT vertex is outside supported half-float range.` | During the animation, a vertex of an animated mesh is more than about 31 km from the Animator's object, or its position isn't a finite number. | Keep the animated meshes near the Animator's object, and check the scale of its hierarchy. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:221-222 --> |
| `Layered Lit '<material>': '<option>' is not supported by the mod material format.` | The Layered Lit material uses an option that a map can't store. `<option>` is the shader property: `_UseHeightBasedBlend` is **Use Height Based Blend**, `_UseMainLayerInfluence` is **Main Layer Influence**, `_UVBlendMask` is a **BlendMask UV Mapping** other than **UV0**, and `_UseDensityMode` is the density blend mode. | In the material's **Inspector**, clear the option, or set **BlendMask UV Mapping** to **UV0**. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/LayeredPbrWriter.cs:10-25 --> |
| `Layered Lit '<material>': layer <n> requires UV0 mapping for mod export.` | Layer `<n>` of the material has a **Base UV mapping** other than **UV0**. Layer 0 is the main layer. | Set **Base UV mapping** of that layer to **UV0**. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/LayeredPbrWriter.cs:65-68 --> |
| `meshoptimizer is unavailable for this Editor platform. Install its native Editor library or disable build optimization.` | **Optimize build** is on in a Unity Editor that isn't Windows x64. | Clear **Optimize build**. [Details](#msg-meshoptimizer) |
| `Invalid or extreme vertex coordinates in '<mesh>'.` | With **Optimize build** on, a static mesh has a vertex more than 10 000 km from the world origin, or one whose position isn't a finite number. `<mesh>` is the name of the mesh, not of the object. | Find the objects that use the mesh, and correct their position or scale, or the mesh in your 3D tool. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneExportOptimization.cs:301-303 --> |
| `A triangle spans too many sectors. Increase the optimization sector size or fix the source mesh.` | With **Optimize build** on, a static mesh has a triangle so large that cutting it into sectors would make more than 100 000 pieces. | Split the large triangles in your 3D tool. For a render mesh, a larger **Sector size (m)** also helps. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SectorMeshGeometry.cs:167-169 --> |
| `Binary mesh block exceeds 256 MiB. Split the source mesh.` | In a Binary build, one exported mesh takes more than 256 MiB before compression, for example a single mesh with millions of vertices. | Split the mesh into several meshes in your 3D tool, then select the **Build map** button. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/Binary/BinaryModModelCodecV2.cs:15,206-208; Assets/Plugins/CarX.Modding.Creator/Editor/UnityGoObjExporter.cs:189 --> |
| `Shader 'Hidden/ConvertingEx' is not found.` or `VAT normal export shader is missing.` | A shader of the SDK in `Assets/Plugins/CarX.Modding.Creator` is missing. | Update the submodule, as in [The submodule folder is empty](#submodule-empty). <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/ExportTextureUtility.cs:166-170; Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:274 --> |
| `Invalid binary texture <width>x<height> (<encoding>): sides must be within 1..8192.` | When you export or publish a Binary map, a material texture is wider or taller than 8192 pixels as Unity imports it. Packing keeps each texture at its imported size and doesn't scale it down, so rebuilding with `Everything` doesn't help. | In the texture's import settings, on the **Default** tab, set **Max Size** to 8192 or less and select **Apply**. Then select the **Build map** button, and export or publish again. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/Binary/BinaryTexture.cs:17,55-60; Assets/Plugins/CarX.Modding.Creator/Editor/BinaryTextureBaker.cs:32; Assets/Plugins/CarX.Modding.Creator/Editor/BinaryTexturePreparation.cs:228 --> |
| `Binary map requires both Meta and Binary Map staging data. Rebuild Map and Meta.`, or another message from packing, such as `Missing map texture: <path>` | When you export or publish a Binary map, packing finds the build cache incomplete. | Set **Rebuild targets** to `Everything`, select the **Build map** button, then export or publish again. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/BinaryMapPacker.cs:60-62 --> |
| Any other exception during a build | An internal error of the exporter. | Select the **Build map** button again. If the error repeats, open an issue in the [issue tracker](https://github.com/CarXTechnologies/dro-map-uploader/issues) with the full Console text. |

Export also logs the following warnings in the Console. They don't stop the build, but the map in the game differs from the scene. A warning that repeats a validation error, such as `LODGroup has more than 8 LOD - Not supported`, doesn't appear, because the error stops the build before export. <!-- src: Assets/Editor/MapBuilder.cs:138; Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:137-141 -->

| Message | Cause | Fix |
| --- | --- | --- |
| `Rigidbody '<name>' was skipped: no enabled supported colliders. Geometry and animation will still be exported.` | A Rigidbody has no enabled Box, Sphere, Capsule or Mesh collider of its own on an active object. | Add an enabled collider to the Rigidbody's object. [Details](#msg-rigidbody-skipped) |
| `Rigidbody '<name>': the client uses Unity Physics discrete collision detection; PhysX CCD modes are not exported.` | **Collision Detection** on the Rigidbody is set to a continuous mode. | Set **Collision Detection** to **Discrete**. [Details](#msg-rigidbody-discrete) |
| `Rigidbody '<name>': Multiply material combine uses the Unity Physics geometric mean approximation.` | A collider of a dynamic object has a physics material with **Multiply** for **Friction Combine** or **Bounce Combine**. The game uses a geometric mean instead. | Choose another combine mode, or drive the map to check how the object behaves. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:85-86 --> |
| `[ObjExporter] Material '<material>' property '<property>' holds a <type>, only Texture2D can be exported. Skipped.` | A texture slot of a material holds a cubemap, a Render Texture or another texture that isn't a Texture2D. Export leaves the slot empty. | Assign an imported image (Texture2D) to the slot, or clear it. [Details](#materials-wrong) <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlTextureMapWriter.cs:391 --> |
| `Layered Lit '<material>': transparent blending mode <mode> is not supported by the mod material format, exported as Alpha.` | A transparent Layered Lit material uses the **Additive** or **Premultiply** **Blending Mode**; `<mode>` is `1` or `4`. The game blends it by alpha. | In the material's **Inspector**, set **Blending Mode** to **Alpha**, so that the editor shows the material as the game does. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/LayeredPbrWriter.cs:105-118 --> |
| `Optimization skipped '<name>': mesh must be readable and use triangle topology. Original export is retained.` | With **Optimize build** on, the mesh of a static renderer or MeshCollider isn't readable or has a submesh that isn't made of triangles. The build turns on **Read/Write** for imported models itself, so this concerns a mesh saved as a Unity asset, or a mesh of quads, lines or points. The object keeps its own mesh and isn't cut into sectors. | Nothing is required. To have the object optimized, export the mesh with triangles from your 3D tool and import it as a model. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneExportOptimization.cs:273-281; Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:11-33 --> |
| `Light '<name>' of type <type> is not supported - only Point and Spot lights are exported` | An enabled light isn't a Point or Spot light, for example the Directional light of the preview lighting. Validation reports the same light as a note or a warning. | Nothing for a Directional light: the game uses its own sun. Replace a light of another type with a Point or Spot light. See [Set up lighting](scene-setup.md#lighting). <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:90-101 --> |
| `Material '<material>': its render core values were exported from material for revision <n> of <variant>, the current revision is <n>. The game uses the fallback material until you re-run Export from material on its Material marker.` | A Material marker entry holds values exported for an older revision of its render core variant. Validation shows the same problem as a Markers warning. | In the Material marker, select **Export from material** for that entry, then build the map again. [Details](#msg-render-core-revision) <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RenderCoreMaterialWriter.cs:36-41 --> |
| `[ModExport] Previous export did not restore import settings of <n> textures and <n> models; they will be restored at the end of this export.` | The previous build ended before it restored the import settings it had changed, for example because Unity closed during the build. A build turns on **Read/Write** and turns off compression for the textures and models it exports, and restores them at the end. | Nothing: this build restores them. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/ExportImporterSettings.cs:186-199 --> |

<a name="msg-rigidbody-lodgroup"></a>
### `Rigidbody '<name>' cannot split an enclosing LODGroup. Put the…`

Message: `Rigidbody '<name>' cannot split an enclosing LODGroup. Put the Rigidbody on the LODGroup root.`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:39 -->

**Cause.** A Rigidbody is on a child inside a LODGroup's hierarchy instead of on the object that has the LODGroup, so the moving body would take only part of the LOD group with it.

**Fix.**

- Move the Rigidbody to the object that has the LODGroup component.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

<a name="msg-rigidbody-sheared"></a>
### `Rigidbody '<name>': collider '<object>' has a sheared transform. Apply…`

Message: `Rigidbody '<name>': collider '<object>' has a sheared transform. Apply scale in the model first.`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:68 -->

**Cause.** A collider of a dynamic object has a sheared transform. This happens when a rotated child sits inside a parent whose X, Y and Z scale differ, and the game can't reproduce that collider shape.

**Fix.**

1. Set the scale of every parent of the named collider to the same value on X, Y and Z. If the model needs a non-uniform scale, apply it to the mesh in your 3D tool and import the model again.
1. Select the **Build map** button.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

<a name="msg-rigidbody-convex"></a>
### `Rigidbody '<name>': a dynamic MeshCollider must be Convex.`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:76 -->

**Cause.** A Rigidbody with **Is Kinematic** cleared owns a MeshCollider with **Convex** cleared. Validation usually reports the same problem in the Physics category. A MeshCollider without a mesh on a dynamic object stops the build with `Rigidbody '<name>': MeshCollider has no mesh.`

**Fix.**

1. On the MeshCollider, select **Convex**.
1. Make sure that **Mesh** on the MeshCollider has a mesh assigned.
1. Select the **Build map** button.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

<a name="msg-rigidbody-in-animation"></a>
### `Put Rigidbody on the Animator root or above it; a baked animation…`

Message: `Put Rigidbody on the Animator root or above it; a baked animation cannot contain independent moving bodies.`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:46 -->

**Cause.** A Rigidbody is inside a hierarchy that an Animation marker bakes into vertex animation. A baked animation can't contain bodies that move on their own.

**Fix.**

- Move the Rigidbody to the Animator's object or to one of its parents.

See also: [Animate objects with vertex animation](markers.md#animation)

<a name="msg-vat-too-large"></a>
### `VAT for '<name>' is too large (<width>x<height>). Reduce vertices…`

Message: `VAT for '<name>' is too large (<width>x<height>). Reduce vertices, clips or sample rate.`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:128 -->

**Cause.** The texture that stores the baked vertex animation would be too large. Its size grows with the vertex count of the animated meshes, the total length of the clips and **Bake frames per second**.

**Fix.**

1. In the Animation marker, lower **Bake frames per second** (default 30).
1. If the error remains, shorten or remove clips, or reduce the vertex count of the animated meshes.
1. Select the **Build map** button.

See also: [Animate objects with vertex animation](markers.md#animation)

<a name="msg-meshoptimizer"></a>
### `meshoptimizer is unavailable for this Editor platform. Install its…`

Message: `meshoptimizer is unavailable for this Editor platform. Install its native Editor library or disable build optimization.`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneExportOptimization.cs:177 -->

**Cause.** **Optimize build** is on, and the Editor runs on a platform without the meshoptimizer native library. The project ships it only for the Windows x64 Editor.

**Fix.**

1. In the **MapBuilder** window, on the **Build** tab, select **Optimization**.
1. Clear **Optimize build**.
1. Select the **Build map** button.

Validation then shows the warning `Build optimization is off: …`, which is expected on this platform.

See also: [Optimization](build.md#optimization)

<a name="msg-rigidbody-skipped"></a>
### `Rigidbody '<name>' was skipped: no enabled supported colliders.…`

Message: `Rigidbody '<name>' was skipped: no enabled supported colliders. Geometry and animation will still be exported.`

Severity: Warning (the build continues) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:94 -->

**Cause.** A Rigidbody has no enabled Box, Sphere, Capsule or Mesh collider of its own on an active object. Its geometry is still exported, but it doesn't move.

**Fix.**

- Add an enabled collider to the Rigidbody's object, or to an active child that isn't inside another Rigidbody.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

<a name="msg-rigidbody-discrete"></a>
### `Rigidbody '<name>': the client uses Unity Physics discrete collision…`

Message: `Rigidbody '<name>': the client uses Unity Physics discrete collision detection; PhysX CCD modes are not exported.`

Severity: Warning (the build continues) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:41 -->

**Cause.** **Collision Detection** on the Rigidbody is set to a continuous mode. The game always uses discrete collision detection, so this setting isn't exported.

**Fix.**

- On the Rigidbody, set **Collision Detection** to **Discrete**, so that the editor behaves like the game.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

<a name="greyed-out"></a>
## Greyed-out controls

A control in the **MapBuilder** window is unavailable until its conditions are met. While a build or an upload runs, **LOCAL MAPS**, the **Map** tab, the build settings and **Publications** are unavailable too; wait for the operation to finish, or select **Cancel** in the footer.

| Control | Unavailable when | What to do |
| --- | --- | --- |
| **Validate map** | An operation is running, or publication logos are still downloading after you sign in. | Wait until the operation or the downloads finish. |
| **Build map** button | The same as **Validate map**, or **Rebuild targets** under **Advanced settings** is `Nothing`. | Set **Rebuild targets** to `Everything`. |
| **Scene** | **Rebuild targets** doesn't include `Map`. | Add `Map` to **Rebuild targets**. |
| **Open build folder** | The map has no build cache folder. | Build the map. |
| **Export to folder** button | The build doesn't match the map settings, **Scene** and **Format**, or **Folder** isn't an absolute path. | Build the map with the selected **Scene** and **Format**, and enter a full path that starts with a drive letter, such as `C:/Games/CarX Drift Racing Online 2/Mods/MyTrack`. |
| **Create publication** | You aren't signed in, or the build doesn't match the map settings, **Scene** and **Format**. | Sign in, then build the map. |
| **Update publication** | You aren't signed in, no publication linked to this map is selected in **Publications**, or the build doesn't match. | Sign in, select the publication's row, then build the map again. |
| **Cancel** | Sending to mod.io has started. The tooltip reads `Sending to the platform cannot be cancelled once started`. | Wait for the upload to finish. |
| **Sign in** | Sign-in is in progress, or an operation is running. | Finish the sign-in dialogs, or wait for the operation to finish. |
| **EN**/**RU** dropdown, **Sign out** | An operation is running. | Wait for the operation to finish. |
| **Platform** in the header | Always: mod.io is the only platform. | Nothing; the field only shows the platform. |

When no map is selected in **LOCAL MAPS**, the **Build** and **Publish** tabs stay empty and show `Select a local map or add a new one.` <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:624 -->

<a name="build-state"></a>
## Build state

<a name="msg-rebuild-needed"></a>
### `Rebuild needed`

Shown in: the map's row in **LOCAL MAPS** <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:198 -->

**Cause.** The map was built before, but the build no longer matches the map. This happens when:

- you edited a field on the **Map** tab after the build;
- you changed a setting under **Optimization**, which discards the Map part of the build;
- you created a publication, which discards the Meta part, because that build's metadata still carries a placeholder ID;
- the build cache folder is gone, for example because Windows cleaned its temp folder or you opened the project on another computer;
- a later build of only Map or only Meta failed.

On the **Build** tab, the message depends on what happened to the build: <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:819-832 -->

- When both parts are built but no longer match, after an edit on the **Map** tab or when the build cache is gone, it reads `The build is outdated or its files are missing. Build the selected scene in the current format.` The same message appears when you change **Scene** or **Format** after the build, while the row in **LOCAL MAPS** still shows `Ready to export`.
- When one part was discarded, after an **Optimization** change, **Create publication** or a failed build of only that part, it reads `Map geometry has not been built yet.` or `Map metadata has not been built yet.`

**Fix.**

- In the **MapBuilder** window, on the **Build** tab, select the **Build map** button.

See also: [Where the build goes](build.md#cache) · [Create a publication](publish.md#create)

<a name="msg-build-incomplete"></a>
### `Build incomplete. Check validation results and the Console.`

Shown in: the build summary on the **Build** tab <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:387 -->

**Cause.** After the last build, Map and Meta aren't both built: a validation error, an exception during export or **Cancel** stopped it. A build discards the parts it rebuilds before it starts, even after earlier successful builds. When the failed build had both Map and Meta in **Rebuild targets**, as by default, the map's row in **LOCAL MAPS** shows `Not built`; when only one part failed, it shows `Rebuild needed`. <!-- src: Assets/Editor/MapBuilder.cs:271 -->

**Fix.**

1. Fix the errors in the **Map Validation** window. It opens by itself after a build that found problems.
1. Fix the errors in the Console, as described in [Build errors in the Console](#build-errors).
1. Select the **Build map** button.

See also: [Fix validation problems](validation.md)

<a name="game-problems"></a>
## Problems in the game

Local maps load from the `Mods` folder next to the game executable, only on PC and only in game builds with mod support. To find out whether your copy of the game loads them, see [Check that the game loads local maps](test-in-game.md#check-game). To export and load a map, see [Test the map in the game](test-in-game.md).

<a name="map-missing"></a>
### The map doesn't appear in the game

**Cause.** The game lists a local map only when it finds a non-empty folder directly in `<game folder>/Mods/`, where `<game folder>` is the folder with the game executable. Every subfolder is one map, and folders whose names start with `.` are skipped. The **Mods** tab of the **Track Selection** screen appears only when the game finds at least one map. <!-- src: game -->

**Fix.**

1. Check the path. The map's files must sit directly in `<game folder>/Mods/<MapName>/`, for example `<game folder>/Mods/<MapName>/mod.cxmod` for a Binary build. Files one folder deeper don't load.
1. Check that the folder isn't empty. When an export fails, the **Publish** tab shows `Nothing to export — see the Console.`
1. In the game, open the **Track Selection** screen again. The game reads the `Mods` folder again each time it lists maps.
1. If the map still doesn't appear, to find out whether your copy of the game loads local maps, export the TestMap sample, as described in [Check that the game loads local maps](test-in-game.md#check-game).

See also: [Where the game looks for local maps](test-in-game.md#local-maps) · [Check that the game loads local maps](test-in-game.md#check-game)

<a name="map-twice"></a>
### The map appears twice after you publish it

**Cause.** The local copy in `<game folder>/Mods/<MapName>/` and the copy installed from mod.io have different mod IDs: the folder name, and `<modIoId>_<modfileId>`. The game lists both. <!-- src: game -->

**Fix.**

- Delete the folder `<game folder>/Mods/<MapName>/`.

See also: [Remove the local copy after you publish](test-in-game.md#after-publish)

<a name="car-falls-through"></a>
### The car falls through the map

**Cause.** One of these:

- The surface has a render mesh but no supported collider. Only a MeshCollider with a mesh, and enabled BoxCollider, SphereCollider and CapsuleCollider components that aren't triggers, are exported. A MeshCollider is exported even when it is disabled or a trigger. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:304-307,339 -->
- The surface is a Unity Terrain. Terrain and TerrainCollider aren't exported, even when a Road marker on the terrain passes validation.
- A MeshCollider has no mesh. Validation warns: `'<object>' has a MeshCollider with no mesh assigned, so it collides with nothing.`
- The surface is inside an object tagged `Garbage`, which leaves it out of the export.

**Fix.**

1. Give every drivable surface a supported collider outside any `Garbage` subtree: a MeshCollider with a mesh assigned, or an enabled BoxCollider, SphereCollider or CapsuleCollider with **Is Trigger** cleared.
1. Replace any terrain with a mesh that has a MeshCollider.
1. Build the map and export it again.

See also: [Add colliders](scene-setup.md#colliders) · [Unity Terrain isn't exported](scene-setup.md#before-you-start)

<a name="road-grip"></a>
### A road surface has the wrong grip

**Cause.** One of these:

- The Road marker and the collider are on different GameObjects. The game applies a Road marker's surface only to a collider on the marker's own object; any other collider gets the default physics material. Validation accepts a collider on a child, so it doesn't report this.
- The marker's surface is `default`, `ice_snowy` or `pavement`. The game has no surface template with these names, so the collider gets no road surface, with values or without, and behaves like a collider without a Road marker. The game log, `Player.log`, then shows a warning that starts with `Road marker references surface template '<name>'`. Validation warns about such a marker; see [its message](#msg-road-unavailable-surface).
- The marker has a **Template Config**. Its values, from the named template or from **Custom**, replace the game's surface entirely, **Surface Type** and **Water Rate** included. They start from the MapUploader surface defaults, and these differ from the game's surface on some surfaces, for example **Bump Min** of `grass`. When the marker's template is removed from the config, the marker exports its own **Custom** values. <!-- src: Assets/Scripts/GameMarkerData.cs:14-37 -->

**Fix.**

1. Put each Road marker on the GameObject that has the collider.
1. Replace the surfaces that the game doesn't have: in the type menu of the marker, select **Road** > **asphalt** instead of `default`, **Road** > **road_icy** instead of `ice_snowy`, and **Road** > **paving_slab** instead of `pavement`.
1. To use the game's own surface, set **Template Config** on the marker to `None`. To tune the surface instead, keep the **Template Config** and set every field of the template or of **Custom**, including **Surface Type** and **Water Rate**.
1. Build the map and export it again.

See also: [Mark road surfaces](markers.md#road) · [When the physics values apply](reference.md#surface-values) · [Reuse road values with templates](markers.md#templates)

<a name="materials-wrong"></a>
### Materials are magenta, white or untextured

**Cause.** One of these:

- A MeshRenderer has an empty material slot; the game draws it magenta. Validation warns about it.
- A render core material from a Material marker has no diffuse texture; the game draws it white. Validation warns about it.
- A material texture isn't a Texture2D, for example a render texture. Export skips it and logs `[ObjExporter] Material '<material>' property '<property>' holds a <type>, only Texture2D can be exported. Skipped.` in the Console. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlTextureMapWriter.cs:391 -->

**Fix.**

1. Fix the material as the validation message or the Console line describes.
1. Build the map and export it again.

See also: [Set up materials](scene-setup.md#materials)

<a name="lights-off"></a>
### Lights stay off in the daytime

**Cause.** This is expected. In the game, exported Point and Spot lights turn on only at night. Directional and Area lights are never exported; the game supplies its own sun and sky. <!-- src: game -->

**Fix.**

- To check your lights, on the **Mods** screen in the game, select a night time, such as **00:00**, before you start the drive.

See also: [Set up lighting](scene-setup.md#lighting)

<a name="spawn-names"></a>
### Spawn points show numbers or wrong names on the world map

**Cause.** The world map in the game lists every spawn point as a fast-travel point under its GameObject name. A spawn point with an empty name shows its number instead. <!-- src: game -->

**Fix.**

1. Give each spawn point GameObject a unique, meaningful name. Duplicate names give the validation warning `<n> spawn points are all named '<name>'. Wavefront/Binary exports the object name as the spawn point's identity, so give each one a distinct, meaningful name.`
1. Build the map and export it again.

See also: [Add spawn points](markers.md#spawn)

<a name="minimap-wrong"></a>
### The minimap is offset, stretched or missing

**Cause.** One of these:

- **Bounds Center** or **Bounds Size** doesn't match the image. **Bounds Center** is the world X and Z of the image centre; its **Y** field holds world Z. **Bounds Size** is the world X and Z size that the image covers. The top of the image points to world +Z.
- The image's aspect ratio differs from **Bounds Size** X : Y. The game stretches the image to the rectangle.
- The object with the Minimap component is inactive. An inactive Minimap passes validation, but it isn't exported.
- The image you expect is in a **Textures** element other than element 0. The game shows only element 0.

**Fix.**

1. Correct the Minimap component or the image.
1. Build the map and export it again.

See also: [Add the minimap](minimap-and-images.md#minimap)

<a name="msg-load-failed"></a>
### `Failed to load the track mod. Please try again.`

**Cause.** The game found the map folder but couldn't load the map, or loading took too long. Very large maps may fail to load. <!-- src: game -->

**Fix.**

1. Optional: To make sure that no map file is locked, close the game.
1. Delete everything in `<game folder>/Mods/<MapName>/`, then export the map into the empty folder again.
1. In the game, load the map. If you closed the game, start it first.

If loading still fails, open the game log, `%USERPROFILE%\AppData\LocalLow\CarX Technologies\CarX Drift Racing Online 2\Player.log`, and find `Failed to load mod '<MapName>'`. The exception that follows names the reason. <!-- src: game -->

See also: [Load the map in the game](test-in-game.md#in-game)

<a name="join-local-map"></a>
### Other players can't join a room on your local map

**Cause.** A room on a local map is open only to players who have a map folder with the same name in their own `Mods` folder. Other players don't see the room in their room list, and joining it by code shows `Mod Not Installed` with `Install the track mod to join this room.` A map published on mod.io downloads when a player joins, in game builds that support mod.io. <!-- src: game -->

**Fix.**

- To test with friends, give them a copy of your exported map folder. They put it in their `<game folder>/Mods/` under the same folder name.
- To share the map with everyone, publish it to mod.io.

See also: [Test the map in the game](test-in-game.md) · [Publish to mod.io](publish.md)

<a name="msg-room-version"></a>
### `The room uses a different version of this track mod. Update the…`

Message: `The room uses a different version of this track mod. Update the mod or choose another room.`

**Cause.** Every **Update publication** uploads a new file, and a room on a mod.io map accepts only players who have the same version installed as the host. <!-- src: game -->

**Fix.**

- Wait until the game has updated the map from mod.io for the host and for the players, then create or join the room again.

See also: [Update a publication](publish.md#update)

<a name="upload-problems"></a>
## Upload problems

<a name="msg-map-size"></a>
### `'<scene>': the map is <size> mb and this vendor accepts 4096 mb.`

Severity: Error (stops the upload) · Shown in: Console <!-- src: Assets/Editor/MapBuilder.Storage.cs:169-170 -->

**Cause.** Right before **Create publication** and **Update publication**, MapBuilder measures the build cache, and the map part is over its limit. When the metadata part is too large, the message is `'<scene>': the meta is <size> mb and this vendor accepts 24 mb.`; the metadata holds the Icon, the Preview and the minimap images. For the limits and what they measure, see [Build size](reference.md#limits-size).

The footer then shows `Publication did not complete. See the Console.` or `Update did not complete. See the Console.`

**Fix.**

1. For the map part, reduce texture sizes and remove geometry that the map doesn't need. For the metadata part, use smaller Icon, Preview and minimap images.
1. Select the **Build map** button, then publish again.

See also: [Publish to mod.io](publish.md) · [Where the build goes](build.md#cache)

<a name="publications-empty"></a>
### The publications list is empty although you have publications

**Cause.** MapBuilder couldn't read your publications from mod.io, and the Console shows `Could not read your mod.io creations: <reason>`. The list then keeps its previous content, usually `No publications on mod.io yet.` The reason often reads `You have used the input_json parameter with semantically incorrect JSON.`, which doesn't describe the real problem. Two causes are known: <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoPublisher.cs:126 -->

- A server or session error, for example a session from an older mod.io address. The Console then shows `HTTP Code: [502]` in the messages logged right before the error.
- A publication without a file on your account, for example one created on the mod.io website. The mod.io plugin can't read a publication without a file, so the whole list fails.

**Fix.**

1. In the **MapBuilder** window, on the **Publish** tab, in **Publications**, select **Refresh list**.
1. If the list still fails and the Console shows `HTTP Code: [502]` or a sign-in error, in the header of the **MapBuilder** window, select **Sign out**.
1. Sign in again, as described in [Sign in to mod.io](publish.md#sign-in).
1. Select **Refresh list**.

To see the underlying error, optionally:

1. In the **Project** window, select `Assets/Resources/CarX.Modding/ModIoConfig.asset`.
1. In the **Inspector**, set **Log Level** to `Verbose`.
1. In **Publications**, select **Refresh list**, and read the error in the Console.
1. Set **Log Level** back to `Warning`.

If the list still fails after you sign in again, and mod.io shows a publication without a file on your account, delete that publication by its ID. **Delete publication** asks for the ID when no row is selected, so it works without the list: copy the ID from the publication's page on mod.io, follow [Delete a publication that the list can't show](publish.md#delete-by-id), then select **Refresh list**.

> [!CAUTION]
> Deleting removes the publication and every file published to it from mod.io. It can't be undone.

See also: [Publish to mod.io](publish.md) · [Sign in to mod.io](publish.md#sign-in)

<a name="msg-sign-in-failed"></a>
### `mod.io sign in failed: <reason>`

Severity: Error (you stay signed out) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoAuthProvider.cs:103 -->

**Cause.** mod.io rejected the code, for example because it expired, or the connection failed. Two related messages mean that you stopped the sign-in yourself: `The mod.io terms of use were not accepted.` after **Decline**, and `Sign in cancelled.` after you cancel the email prompt.

**Fix.**

1. In the header of the **MapBuilder** window, select **Sign in**.
1. If the `mod.io terms of use` dialog opens, select **Accept**.
1. In **Email**, enter the email address of your mod.io account, and select **Continue**.
1. In **Code**, enter the code from the newest email from mod.io, and select **Continue**.

See also: [Sign in to mod.io](publish.md#sign-in)

<a name="modio-unavailable"></a>
### The header shows `Unavailable`

**Cause.** mod.io couldn't start, usually because there is no connection. The box at the top of the **Publish** tab shows the reason, for example `mod.io failed to initialize: <reason>`. **Validate map**, the **Build map** button and the **Export to folder** button don't need mod.io. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoEditorBootstrap.cs:132 -->

**Fix.**

1. Check your internet connection.
1. Close the **MapBuilder** window and open it again with **Tools** > **MapBuilder**. The window connects to mod.io each time it opens.

See also: [Sign in to mod.io](publish.md#sign-in)

<a name="setup-problems"></a>
## Setup problems

<a name="ssh-publickey"></a>
### `git@github.com: Permission denied (publickey).`

**Cause.** The project registers its submodule `Assets/Plugins/CarX.Modding.Creator` with an SSH address, and your computer has no SSH key for GitHub. <!-- src: .gitmodules:3 -->

**Fix.**

- In a terminal, in the project folder, run this command. It fetches the submodule over HTTPS for this one run and doesn't change your git configuration.

  ```bash
  git -c url."https://github.com/".insteadOf=git@github.com: submodule update --init --recursive
  ```

See also: [Get the project](getting-started.md#clone)

<a name="submodule-empty"></a>
### The submodule folder is empty and the project doesn't compile

**Cause.** `Assets/Plugins/CarX.Modding.Creator` is a git submodule. **Code** > **Download ZIP** on GitHub doesn't include it, and neither does a clone made without `--recurse-submodules`.

**Fix.**

- For a ZIP download, clone the project with git instead, as described in [Get the project](getting-started.md#clone).
- For a clone without the submodule, run `git submodule update --init --recursive` in the project folder.

Both the clone and this command fetch the submodule from its SSH address. If either reports `git@github.com: Permission denied (publickey).`, run the HTTPS command from [Permission denied (publickey)](#ssh-publickey) instead. <!-- src: .gitmodules:3 -->

See also: [Get the project](getting-started.md#clone) · [Update an existing clone](getting-started.md#update-clone)

<a name="unity-version"></a>
### The project opens in a different Unity version

**Cause.** The project is pinned to Unity 6000.3.19f1, revision 7689f4515d75. Opening it in another version upgrades the project files in place. <!-- src: ProjectSettings/ProjectVersion.txt:1-2 -->

**Fix.**

1. Close the Unity Editor.
1. Install Unity 6000.3.19f1 through Unity Hub.
1. In Unity Hub, open the project with 6000.3.19f1.

If the other version already opened the project, it changed files such as `ProjectSettings/ProjectVersion.txt`. Before you open the project again, run `git status` in the project folder, and restore each changed file that you didn't edit yourself with `git restore <path>`.

> [!CAUTION]
> `git restore <path>` discards every local change in that file. Don't run it on your own map files.

See also: [Install Unity 6000.3.19f1](getting-started.md#unity)

<a name="scene-empty"></a>
### The Scene dropdown is empty

**Cause.** **Scene** lists only the scenes that are enabled in the build scene list, and the list has none. MapBuilder then keeps checking and building the scene of the last build, or the scene that was open when you selected the map, which may not be the one you expect. If that scene was never saved, validation reports `No target scene is selected. Pick one in Scene on the Build tab.` A scene under `Assets/MapResources` is added and enabled whenever the editor loads or you save the scene, and a scene you disable in the list is dropped from it at the next save. <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:780-786; Assets/Editor/MapBuilderEditorWindow.Workspace.cs:228-229; Assets/Editor/Validation/MapMetaValidator.cs:33; Assets/Editor/MapBuilderProcessor.cs:79 -->

**Fix.**

1. Move the scene into `Assets/MapResources/<MapName>/`.
1. Open the scene and save it.
1. In the **MapBuilder** window, on the **Build** tab, select the scene in **Scene**.

A scene outside `Assets/MapResources` has to be added and enabled by hand: select **File** > **Build Profiles**, then, in the **Build Profiles** window, select **Scene List** and add the scene.

See also: [Choose the scene and format](build.md#format)

<a name="faq"></a>
## Common questions

### Can I use Unity Terrain?

Not directly. Terrain and TerrainCollider aren't exported, so the car falls through them in the game. Model the ground as a mesh with a MeshCollider instead. See [Unity Terrain isn't exported](scene-setup.md#before-you-start) and [Import models](scene-setup.md#import).

### I work on macOS or Linux. Can I build maps?

The build optimizer ships only for the Windows x64 Editor. On another system, turn off **Optimize build** for each map under **Optimization** on the **Build** tab, or the build stops with an error. To test the map, you still need the game on a Windows PC. See [Get started](getting-started.md) and [Optimization](build.md#optimization).

### How many spawn points can I add, and what are they for?

Add as many as you need; at least one is required. Every spawn point becomes a fast-travel point on the in-game world map, listed under its GameObject name, and the car starts at the first one. See [Add spawn points](markers.md#spawn).

### Why are my lights off?

Exported Point and Spot lights turn on only at night in the game, and Directional and Area lights are never exported. See [Set up lighting](scene-setup.md#lighting).

### Do I need a mod.io account to build or test my map?

No. **Validate map**, the **Build map** button and the **Export to folder** button work without signing in; only publishing needs a mod.io account. See [Sign in to mod.io](publish.md#sign-in).

### Can friends join my local map?

Only if they have a copy of your map folder under the same name in their own `Mods` folder. To share the map with everyone, publish it to mod.io. See [Test the map in the game](test-in-game.md).

### Which platforms can download my map?

Players get maps from mod.io in game builds that support mod.io. Local maps from the `Mods` folder work only on PC. See [Publish to mod.io](publish.md).

### How do I make my publication public?

**Create publication** creates the publication hidden, and **Update publication** never changes its visibility. Change the visibility on the publication's page on mod.io; the tooltip of the size line in its **Publications** row gives the page address. See [Make the publication public](publish.md#make-public).

### How do I remove the local copy after publishing?

Delete `<game folder>/Mods/<MapName>/`; otherwise the game lists the map twice. See [Remove the local copy after you publish](test-in-game.md#after-publish).

### How do I rename the map?

Change **Name** on the **Map** tab, build the map again, then select **Update publication** with **Update title** selected. See [Update a publication](publish.md#update).

### Where is my built map file?

A build goes to the build cache, not into a game package: `mod.cxmod` is created only when you export or publish. **Open build folder** on the **Build** tab opens the cache. See [Where the build goes](build.md#cache).

### How do I move the project to another computer?

Copy or commit the whole project, including `Assets/Resources/MapManagerConfig.asset`, which holds the publication links, build records, changelogs and update toggles. The interface language, **Binary textures**, build summaries and your mod.io email stay in the old computer's EditorPrefs, and the build cache stays in its temp folder. On the new computer, sign in again and build the map again. See [Where settings and builds are stored](reference.md#storage).

## Next steps

← [Publish to mod.io](publish.md) · [Contents](README.md) · [Reference](reference.md) →
