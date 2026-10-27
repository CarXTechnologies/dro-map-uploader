# Troubleshooting

**English** · [Русский](../ru/troubleshooting.md)

Applies to: MapUploader `feature/2.0/3.0` · Unity 6000.3.19f1 · screenshots 2026-10-26

Run **Validate map**, fix the errors from the top, then build again.

Find the message or the symptom you see, and apply its fix. Messages are quoted in English, exactly as the tools show them; placeholders such as `<obj>` stand for your object name, file name or number.

## Validation messages

The **Map Validation** window lists these messages after you select **Validate map** or the **Build map** button. Errors block the build; warnings and notes don't. Most messages have a **Select** button that selects the object or asset the message is about. To read the window and use its filters, see [Fix validation problems](validation.md#results).

### `The map has no SpawnPoint marker, so there is nowhere for a car…`

Severity: Error (blocks the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:281-283 -->

**Cause.** No GameMarkerData component in the scene is set to **SpawnPoint**. Markers inside an object tagged `Garbage` don't count. When the scene has no GameMarkerData at all, the window also shows `The map has no GameMarkerData at all - at minimum it needs a spawn point.` and `'GameMarkerData': the map has 0, at least 1 is required.`

**Fix.**

1. In the **Project** window, open `Assets/ExamplePrefab/` and drag `SpawnPoint.prefab` into the scene.
1. Move the new object to the place where the car should appear.
1. Rename the object. The world map in the game lists every spawn point under its GameObject name.

See also: [Add spawn points](markers.md#spawn)

### `'<obj>' is marked as Road but has no Collider on it or its children…`

Severity: Error (blocks the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:266-268 -->

**Cause.** A GameMarkerData set to a **Road** surface is on an object that has no collider, neither on the object itself nor on a child. A render mesh alone gives the car nothing to drive on.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, select **Add Component** and add a MeshCollider, BoxCollider, SphereCollider or CapsuleCollider to the same object.

> [!IMPORTANT]
> Keep the Road marker and the collider on the same GameObject. Validation also accepts a collider on a child, but the game gives that child collider no surface.

See also: [Mark road surfaces](markers.md#road)

### `'<obj>' uses unsupported marker '<head>'. This marker will be skipped…`

Severity: Warning (doesn't block the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:240-242 -->

**Cause.** The GameMarkerData uses a marker type that the game no longer reads: `Ambient`, `TimeObjectActivator` or `NetworkObject`. The **Inspector** shows the same problem as `Unsupported marker: <head>. Select SpawnPoint, Road, Animation or Material, or remove the component.` The example prefab `Assets/ExamplePrefab/Ambient.prefab` and the TestMap sample carry such markers.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in the context menu of the GameMarkerData component, select **Remove Component**.

If the object should carry a marker, select **SpawnPoint**, a **Road** surface, **Animation** or **Material** in the type menu instead.

See also: [Remove unsupported markers](markers.md#unsupported)

### `Animation marker requires an Animator with animation clips.`

Severity: Error (blocks the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:255-256 -->

**Cause.** An Animation marker has no Animator: neither in its **Animator** field nor on its own GameObject. Or the Animator has no controller, or its controller has no clips.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in the Animation marker, assign to **Animator** the Animator at the root of the animated hierarchy.
1. On that Animator, assign a controller that has at least one clip.

See also: [Animate objects with vertex animation](markers.md#animation)

### `'<Type>' on '<obj>' is not a supported component and will not be…`

Severity: Note (doesn't block the build) · Category: Components <!-- src: Assets/Editor/Validation/SceneValidator.cs:174-176 -->

**Cause.** The object carries a component that a map can't store, for example Terrain, TerrainCollider, AudioSource, a Camera or a particle system. Export skips that component and keeps the supported components on the same object.

**Fix.** Nothing is required unless the skipped component matters for driving:

- For Terrain, replace the terrain with a mesh that has a MeshCollider. Unity Terrain isn't exported, so the car falls through it in the game.
- To hide the note for a component type you keep on purpose, add the type name, for example `AudioSource`, to the **Valid** list of `Assets/Resources/MapSkipComponentConfig.asset`.
- To leave a whole helper object out of validation and export, set its **Tag** to `Garbage`.

See also: [Import models](scene-setup.md#import) · [Hide notes for a component type](validation.md#skip-types) · [Organize the scene](scene-setup.md#garbage)

### `'<Type>' on '<obj>' requires a GameMarkerData Animation marker on…`

Severity: Note (doesn't block the build) · Category: Components <!-- src: Assets/Editor/Validation/SceneValidator.cs:163-165 -->

**Cause.** An Animator or a SkinnedMeshRenderer isn't inside a hierarchy that an Animation marker bakes. Export skips the component: the object doesn't move in the game, and a mesh that only a SkinnedMeshRenderer draws isn't exported at all.

**Fix.**

1. In the **Hierarchy**, select the GameObject with the Animator at the root of the animated hierarchy.
1. Add the GameMarkerData component and select **Animation** in its type menu.

See also: [Animate objects with vertex animation](markers.md#animation)

### `'<obj>' has a non-convex MeshCollider driven by a non-kinematic…`

Severity: Error (blocks the build) · Category: Physics <!-- src: Assets/Editor/Validation/SceneValidator.cs:845-847 -->

**Cause.** A MeshCollider with **Convex** cleared is on a Rigidbody's object or inside it, and the Rigidbody has **Is Kinematic** cleared. The game's physics can't move a non-convex mesh collider.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, on the MeshCollider, select **Convex**.

If the object must never move, select **Is Kinematic** on its Rigidbody instead.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

### `'<obj>': Bound Size is <size>. Set it to the size of the map in…`

Severity: Error (blocks the build) · Category: Minimap <!-- src: Assets/Editor/Validation/SceneValidator.cs:900-902 -->

**Cause.** The X or the Y value of **Bounds Size** on the Minimap component is zero. The message calls the field `Bound Size`; in the **Inspector** it is **Bounds Size**. Without a size, the minimap collapses to a point in the game.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in **Bounds Size**, enter the world area that the minimap image covers, in metres: **X** is the world X size and **Y** is the world Z size.

To copy the object's world X and Z scale into **Bounds Size** automatically, select **Lock Size**.

See also: [Add the minimap](minimap-and-images.md#minimap)

### `'<obj>': no textures assigned. Add at least one entry with a Main…`

Severity: Error (blocks the build) · Category: Minimap <!-- src: Assets/Editor/Validation/SceneValidator.cs:908-909 -->

**Cause.** The **Textures** list of the Minimap component is empty. Two related errors have the same fix: `'<obj>': Textures element <i> has no Main Texture assigned.` and `'<obj>': Textures element <i> is a <Type>. Only Texture2D can be exported.`

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in **Textures**, add an element and assign your minimap image, an imported PNG, to its **Main Texture**. The game shows only element 0.

See also: [Capture images with CaptureCamera](minimap-and-images.md#capture)

### `'Minimap': the map has 0, at least 1 is required.`

Severity: Error (blocks the build) · Category: Budget <!-- src: Assets/Editor/Validation/SceneValidator.cs:198-199 -->

**Cause.** The scene has no Minimap component outside `Garbage`. With two or more, the message is `'Minimap': the map has <n>, at most 1 is allowed.` The same two Budget messages appear for every component type whose count is outside its limit, for example too many Transforms or Rigidbodies.

**Fix.**

- Keep exactly one Minimap component in the scene, on an active object outside any `Garbage` subtree. An inactive Minimap passes validation, but it isn't exported.

See also: [Add the minimap](minimap-and-images.md#minimap) · [Supported components and budgets](reference.md#budgets)

### `No Icon is assigned. It is the image shown for the map in game and…`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:103-105 -->

**Cause.** The map has no **Icon**. The Icon becomes the mod.io logo and the map's picture in the game, and mod.io can't create a publication without it. A missing **Preview** is only a warning: `No Preview is assigned. The map will have no large image…`

**Fix.**

- In the **MapBuilder** window, on the **Map** tab, assign a PNG to **Icon · 16:9**.

See also: [Image requirements](minimap-and-images.md#images)

### `The icon '<name>' has Read/Write disabled. Enable it in the texture…`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:111-112 -->

**Cause.** The Icon texture is imported with **Read/Write** turned off. The Preview has the same rule and the message `The preview '<name>' has Read/Write disabled. …` A capture that CaptureCamera saved outside `Assets` and that you then copied into the project has this problem: it gets Unity's default import settings, with **Read/Write** off.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
   The texture is selected in the **Project** window.
1. In the **Inspector**, expand **Advanced** and select **Read/Write**.
1. Select **Apply**.

See also: [Image requirements](minimap-and-images.md#images)

### `The icon is a <ext> file, which is uploaded as is and is not an…`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:165-167 -->

**Cause.** The uploader sends the Icon file to mod.io unchanged, so the Icon and the Preview must be PNG files. A `.jpg` or `.jpeg` file gives only a warning: `The icon is a .jpg file. The uploader uploads the source file as…` A file over the size limit gives `The icon is <X> mb; the limit is 8 mb.`; the Preview limit is 10 MB. <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:27 -->

**Fix.**

1. In your image editor, save the image as PNG within the size limit. mod.io needs a logo of at least 512 × 288.
1. In the **MapBuilder** window, on the **Map** tab, assign the PNG to **Icon · 16:9** or **Preview · 16:9**.

See also: [Image requirements](minimap-and-images.md#images)

### `The scene name '<scene>' cannot be used for a build. Start with a…`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:39-40 -->

**Cause.** The scene file name must start with a letter and use only letters, digits and underscores, for example `MyTrack_01`. Spaces and hyphens aren't allowed. <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:32-45 -->

**Fix.**

1. In the **Project** window, rename the scene file.
1. Open the renamed scene and save it. Saving a scene under `Assets/MapResources` adds it to the build scene list.
1. In the **MapBuilder** window, on the **Build** tab, select the renamed scene in **Scene**.

See also: [Create the map folder and scene](scene-setup.md#scene)

### `The track name '<name>' contains characters that cannot be published.…`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:54-56 -->

**Cause.** **Name** can contain only letters, digits, spaces and `- _ ' . , : ! ? ( ) & + /`, with no space at the start or the end, and at most 50 characters. A longer name gives `The track name is <n> characters; this vendor allows 50.` An empty one gives `The track has no name. Fill in Workshop Name on the map config.`; that message means the **Name** field on the **Map** tab. <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:8; Assets/Resources/CarX.Modding/ModIoConfig.asset:28 -->

**Fix.**

- In the **MapBuilder** window, on the **Map** tab, edit **Name**.

See also: [Fill in the Map tab](map-settings.md#map-tab)

### `The summary is <n> characters; this vendor allows 250.…`

Severity: Error (blocks the build) · Category: Meta <!-- src: Assets/Editor/Validation/MapMetaValidator.cs:91-95 -->

**Cause.** The summary is longer than 250 characters. MapBuilder uses only the first line of **Summary (optional)**; when that field is empty, it uses the first line of **Description**. A **Description** over 50 000 characters gives a similar error: `The description is <n> characters; this vendor allows 50000.` <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:29-30 -->

**Fix.**

- In the **MapBuilder** window, on the **Map** tab, in **Summary (optional)**, enter a single line of at most 250 characters.

See also: [Fill in the Map tab](map-settings.md#map-tab)

### `The scene itself was not validated - fix the configuration first…`

Severity: Note (doesn't block the build) · Category: Meta <!-- src: Assets/Editor/MapBuilder.cs:90-91 -->

**Cause.** The map settings have at least one error. A build stops at the settings and doesn't check the scene, so more errors can appear once you fix them. **Validate map** shows this note too, but it checks the scene anyway.

**Fix.**

1. Fix the Meta errors in the **Map Validation** window.
1. Select **Validate map** to see the remaining problems in the scene.

See also: [Run validation](validation.md#run)

### `'<scene>' was not opened, so only the configuration was checked.`

Severity: Note (doesn't block the build) · Category: Scene <!-- src: Assets/Editor/MapBuilder.cs:390 -->

**Cause.** **Validate map** had to open the scene selected in **Scene**, and you cancelled the prompt to save the scene that was open. Only the map settings were checked. The note also appears, with an empty name (`'' was not opened, …`), when no scene is selected in **Scene**; see [The Scene dropdown is empty](#scene-empty). <!-- src: Assets/Editor/MapBuilder.cs:398-403 -->

**Fix.**

- Select **Validate map** again, and in the prompt, save or discard the changes of the open scene.

See also: [Run validation](validation.md#run)

### `'<obj>' has <n> LOD levels; at most 8 are supported and the whole…`

Severity: Error (blocks the build) · Category: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:813-816 -->

**Cause.** A LODGroup has more than 8 levels. Export would skip the whole group.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, on the LODGroup, remove levels until 8 or fewer remain.

See also: [LOD groups](scene-setup.md#lod)

### `'<obj>' is a <type> light. Wavefront/Binary exports Point and Spot…`

Severity: Warning (doesn't block the build) · Category: Lighting <!-- src: Assets/Editor/Validation/SceneValidator.cs:746-749 -->

**Cause.** An enabled light is neither a Point, a Spot nor a Directional light, for example an Area light. Only Point and Spot lights are exported, so this light lights only the scene in the editor.

**Fix.** To export the light, change its **Type** to **Point** or **Spot**. To keep it for the editor preview only, put it inside an object whose **Tag** is `Garbage`: validation and export skip that subtree, and the light still lights the scene in the editor.

See also: [Set up lighting](scene-setup.md#lighting)

### `'<obj>' is a Directional light. It lights the SDK preview only; the…`

Severity: Note (doesn't block the build) · Category: Lighting <!-- src: Assets/Editor/Validation/SceneValidator.cs:739-742 -->

**Cause.** The scene has an enabled Directional light, usually a preview sun such as `sunlight` from an environment preset. It lights only the scene in the editor, and the game supplies its own sun and sky. With two or more Directional lights, enabled or not, the window also shows the warning `The map has <n> Directional Lights. These affect the SDK preview only;…`

**Fix.** No fix is needed for preview lighting, such as an environment preset from `Assets/Art/Internal/EnvironmentProfiles/`. To keep the report short, put the preview lighting inside an object whose **Tag** is `Garbage`: validation and export skip that subtree, and the lights still light the scene in the editor.

See also: [Set up lighting](scene-setup.md#lighting)

### `Build optimization is off: static meshes and colliders are exported…`

Severity: Warning (doesn't block the build) · Category: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:858-860 -->

**Cause.** **Optimize build** is turned off for this map. Static meshes and colliders are exported as they are, without sectors or LODs, so a large map draws and loads more slowly in the game.

**Fix.**

1. In the **MapBuilder** window, on the **Build** tab, select **Optimization**.
1. Select **Optimize build**.
1. Select the **Build map** button.

On macOS and Linux, leave **Optimize build** off: its native library ships only for the Windows x64 Editor.

See also: [Optimization](build.md#optimization)

### `'<obj>' (<mesh>): static <kind>, <n> triangles, world bounds…`

Severity: Warning (doesn't block the build) · Category: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:874-876 -->

**Cause.** A static render mesh, or an enabled non-convex MeshCollider, is larger than **Sector size (m)** (default 64 m) or has more triangles than **Max triangles per mesh** (default 8192), and the build exports it whole: **Optimize build** is off, **Partition static colliders** is off for a collider, or **Partition static render meshes** is off for a render mesh. With the default settings, this warning doesn't appear. It measures colliders against the same two values, although the build cuts colliders into larger pieces: 128 m and up to 32 768 triangles. Objects inside a Rigidbody, Animator, Animation or LODGroup, objects with a marker other than Road or Material, inactive objects and `Garbage` aren't checked. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/BuildOptimizationSettings.cs:9-19 -->

**Fix.** On the **Build** tab, under **Optimization**, turn on **Optimize build** and the Partition option for this kind of mesh, then build the map and drive it to check the result. If you keep the option off, split the mesh in your 3D tool.

See also: [Optimization](build.md#optimization)

### `'<obj>': render core material '<m>' (<V>) was exported from…`

Severity: Warning (doesn't block the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:442-445 -->

**Cause.** A Material marker entry holds values exported for an older revision of its render core variant than the SDK in this project has. Until you export the entry again, the game uses a fallback material instead of the render core variant.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in the Material marker, find the entry for the named material and select **Export from material**.
1. Select the **Build map** button.

See also: [Replace materials with render core variants](markers.md#material)

### `'<obj>': render core material '<m>' (<V>) has no diffuse texture…`

Severity: Warning (doesn't block the build) · Category: Markers <!-- src: Assets/Editor/Validation/SceneValidator.cs:375-377 -->

**Cause.** A Material marker entry has no diffuse texture, so the game draws the surface white.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, in the Material marker, find the entry for the named material and select **Export from material**.
   The entry takes its textures from the source material.

See also: [Replace materials with render core variants](markers.md#material)

### `'<obj>' has an empty material slot; it will render as magenta in game.`

Severity: Warning (doesn't block the build) · Category: Geometry <!-- src: Assets/Editor/Validation/SceneValidator.cs:784-785 -->

**Cause.** An element of **Materials** on a MeshRenderer has no material assigned.

**Fix.**

1. In the **Map Validation** window, select **Select** next to the message.
1. In the **Inspector**, on the MeshRenderer, assign a material to every element of **Materials**.

See also: [Set up materials](scene-setup.md#materials)

## Build errors in the Console

Some problems are found only while the map is exported. They stop the build with an `InvalidOperationException` in the Console, not in the **Map Validation** window, and the **Build** tab then shows `Build incomplete. Check validation results and the Console.` To open the Console, select **Window** > **General** > **Console**.

### `Rigidbody '<name>' cannot split an enclosing LODGroup. Put the…`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:39 -->

**Cause.** A Rigidbody is on a child inside a LODGroup's hierarchy instead of on the object that has the LODGroup, so the moving body would take only part of the LOD group with it.

**Fix.**

- Move the Rigidbody to the object that has the LODGroup component.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

### `Rigidbody '<name>': collider '<obj>' has a sheared transform. Apply…`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:68 -->

**Cause.** A collider of a dynamic object has a sheared transform. This happens when a rotated child sits inside a parent whose X, Y and Z scale differ, and the game can't reproduce that collider shape.

**Fix.**

1. Set the scale of every parent of the named collider to the same value on X, Y and Z. If the model needs a non-uniform scale, apply it to the mesh in your 3D tool and import the model again.
1. Select the **Build map** button.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

### `Rigidbody '<name>': a dynamic MeshCollider must be Convex.`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:76 -->

**Cause.** A Rigidbody with **Is Kinematic** cleared owns a MeshCollider with **Convex** cleared. Validation usually reports the same problem in the Physics category. A MeshCollider without a mesh on a dynamic object stops the build with `Rigidbody '<name>': MeshCollider has no mesh.`

**Fix.**

1. On the MeshCollider, select **Convex**.
1. Make sure that **Mesh** on the MeshCollider has a mesh assigned.
1. Select the **Build map** button.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

### `Put Rigidbody on the Animator root or above it; a baked animation…`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:46 -->

**Cause.** A Rigidbody is inside a hierarchy that an Animation marker bakes into vertex animation. A baked animation can't contain bodies that move on their own.

**Fix.**

- Move the Rigidbody to the Animator's object or to one of its parents.

See also: [Animate objects with vertex animation](markers.md#animation)

### `VAT for '<name>' is too large (<w>x<h>). Reduce vertices, clips or…`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/VertexAnimationBaker.cs:128 -->

**Cause.** The texture that stores the baked vertex animation would be too large. Its size grows with the vertex count of the animated meshes, the total length of the clips and **Bake frames per second**.

**Fix.**

1. In the Animation marker, lower **Bake frames per second** (default 30).
1. If the error remains, shorten or remove clips, or reduce the vertex count of the animated meshes.
1. Select the **Build map** button.

See also: [Animate objects with vertex animation](markers.md#animation)

### `meshoptimizer is unavailable for this Editor platform. Install its…`

Severity: Error (stops the build) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneExportOptimization.cs:130 -->

**Cause.** **Optimize build** is on, and the Editor runs on a platform without the meshoptimizer native library. The project ships it only for the Windows x64 Editor.

**Fix.**

1. In the **MapBuilder** window, on the **Build** tab, select **Optimization**.
1. Clear **Optimize build**.
1. Select the **Build map** button.

Validation then shows the warning `Build optimization is off: …`, which is expected on this platform.

See also: [Optimization](build.md#optimization)

### `Rigidbody '<name>' was skipped: no enabled supported colliders.…`

Severity: Warning (the build continues) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:94 -->

**Cause.** A Rigidbody has no enabled Box, Sphere, Capsule or Mesh collider of its own on an active object. Its geometry is still exported, but it doesn't move.

**Fix.**

- Add an enabled collider to the Rigidbody's object, or to an active child that isn't inside another Rigidbody.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

### `Rigidbody '<name>': the client uses Unity Physics discrete collision…`

Severity: Warning (the build continues) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/RigidbodyExporter.cs:41 -->

**Cause.** **Collision Detection** on the Rigidbody is set to a continuous mode. The game always uses discrete collision detection, so this setting isn't exported.

**Fix.**

- On the Rigidbody, set **Collision Detection** to **Discrete**, so that the editor behaves like the game.

See also: [Add dynamic objects (Rigidbody)](scene-setup.md#dynamic)

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

When no map is selected in **LOCAL MAPS**, the **Build** and **Publish** tabs stay empty and show `Select a local map or add a new one.` <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:623 -->

## Build state

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

### `Build incomplete. Check validation results and the Console.`

Shown in: the build summary on the **Build** tab <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:387 -->

**Cause.** After the last build, Map and Meta aren't both built: a validation error, an exception during export or **Cancel** stopped it. A build discards the parts it rebuilds before it starts, even after earlier successful builds. When the failed build had both Map and Meta in **Rebuild targets**, as by default, the map's row in **LOCAL MAPS** shows `Not built`; when only one part failed, it shows `Rebuild needed`. <!-- src: Assets/Editor/MapBuilder.cs:276 -->

**Fix.**

1. Fix the errors in the **Map Validation** window. It opens by itself after a build that found problems.
1. Fix the errors in the Console, as described in the Console entries on this page.
1. Select the **Build map** button.

See also: [Fix validation problems](validation.md)

## Problems in the game

Local maps load from the `Mods` folder next to the game executable, only on PC and only in game builds with mod support. To export and load a map, see [Test the map in the game](test-in-game.md).

### The map doesn't appear in the game

**Cause.** The game lists a local map only when it finds a non-empty folder directly in `<game folder>/Mods/`, where `<game folder>` is the folder with the game executable. Every subfolder is one map, and folders whose names start with `.` are skipped. The **Mods** tab of the **Track Selection** screen appears only when the game finds at least one map. <!-- src: DR3PC1:Assets/Modules/CarX.Modding/Runtime/Providers/DefaultCollectionProvider.cs:28 -->

**Fix.**

1. Check the path. The map's files must sit directly in `<game folder>/Mods/<MapName>/`, for example `<game folder>/Mods/<MapName>/mod.cxmod` for a Binary build. Files one folder deeper don't load.
1. Check that the folder isn't empty. When an export fails, the **Publish** tab shows `Nothing to export — see the Console.`
1. In the game, open the **Track Selection** screen again. The game reads the `Mods` folder again each time it lists maps.

See also: [Where the game looks for local maps](test-in-game.md#local-maps)

### The map appears twice after you publish it

**Cause.** The local copy in `<game folder>/Mods/<MapName>/` and the copy installed from mod.io have different mod IDs: the folder name, and `<modIoId>_<modfileId>`. The game lists both. <!-- src: DR3PC1:Assets/DR3/Scripts/States/Controllers/Mods/ModeManagerController.cs:184-201 -->

**Fix.**

- Delete the folder `<game folder>/Mods/<MapName>/`.

See also: [Remove the local copy after you publish](test-in-game.md#after-publish)

### The car falls through the map

**Cause.** One of these:

- The surface has a render mesh but no supported collider. Only a MeshCollider with a mesh, and enabled BoxCollider, SphereCollider and CapsuleCollider components that aren't triggers, are exported. A MeshCollider is exported even when it is disabled or a trigger. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/SceneFormatCollector.cs:304-307,339 -->
- The surface is a Unity Terrain. Terrain and TerrainCollider aren't exported, even when a Road marker on the terrain passes validation.
- A MeshCollider has no mesh. Validation warns: `'<obj>' has a MeshCollider with no mesh assigned, so it collides with nothing.`
- The surface is inside an object tagged `Garbage`, which leaves it out of the export.

**Fix.**

1. Give every drivable surface a supported collider outside any `Garbage` subtree: a MeshCollider with a mesh assigned, or an enabled BoxCollider, SphereCollider or CapsuleCollider with **Is Trigger** cleared.
1. Replace any terrain with a mesh that has a MeshCollider.
1. Build the map and export it again.

See also: [Add colliders](scene-setup.md#colliders) · [Import models](scene-setup.md#import)

### A road surface has the wrong grip

**Cause.** One of these:

- The Road marker and the collider are on different GameObjects. The game applies a Road marker's surface only to a collider on the marker's own object; any other collider gets the default physics material. Validation accepts a collider on a child, so it doesn't report this.
- You edited a road template. A marker takes the template's values only when the **Inspector** displays it.
- The marker carries the surface defaults. They set **Surface Type** to `Asphalt` and **Water Rate** to `0` on every surface, for example on `Road/grass`.

**Fix.**

1. Put each Road marker on the GameObject that has the collider.
1. If you edited a template, select each marker that uses it, so that the **Inspector** displays it once.
1. To set **Surface Type** and **Water Rate**, assign a **Template Config** to the marker and use a template or **Custom**.
1. Build the map and export it again.

See also: [Mark road surfaces](markers.md#road) · [Reuse road values with templates](markers.md#templates)

### Materials are magenta, white or untextured

**Cause.** One of these:

- A MeshRenderer has an empty material slot; the game draws it magenta. Validation warns about it.
- A render core material from a Material marker has no diffuse texture; the game draws it white. Validation warns about it.
- A material texture isn't a Texture2D, for example a render texture. Export skips it and logs `[ObjExporter] Material '<m>' property '<p>' holds a <Type>, only Texture2D can be exported. Skipped.` in the Console. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/MtlTextureMapWriter.cs:391 -->

**Fix.**

1. Fix the material as the validation message or the Console line describes.
1. Build the map and export it again.

See also: [Set up materials](scene-setup.md#materials)

### Lights stay off in the daytime

**Cause.** This is expected. In the game, exported Point and Spot lights turn on only at night. Directional and Area lights are never exported; the game supplies its own sun and sky. <!-- src: DR3PC1:Assets/DR3/Scripts/States/Controllers/Mods/ModLightsDayNightBehaviour.cs:44 -->

**Fix.**

- To check your lights, on the **Mods** screen in the game, select a night time, such as **00:00**, before you start the drive.

See also: [Set up lighting](scene-setup.md#lighting)

### Spawn points show numbers or wrong names on the world map

**Cause.** The world map in the game lists every spawn point as a fast-travel point under its GameObject name. A spawn point with an empty name shows its number instead. <!-- src: DR3PC1:Assets/DR3/Scripts/States/Controllers/Mods/ModWorldMapController.cs:351 -->

**Fix.**

1. Give each spawn point GameObject a unique, meaningful name. Duplicate names give the validation warning `<n> spawn points are all named '<name>'. …`
1. Build the map and export it again.

See also: [Add spawn points](markers.md#spawn)

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

### `Failed to load the track mod. Please try again.`

**Cause.** The game found the map folder but couldn't load the map, or loading took too long. Very large maps may fail to load. <!-- src: DR3PC1:Assets/DR3/Localization/LocalesJson/English(en).json:3609 -->

**Fix.**

1. Optional: To make sure that no map file is locked, close the game.
1. Delete everything in `<game folder>/Mods/<MapName>/`, then export the map into the empty folder again.
1. In the game, load the map. If you closed the game, start it first.

If loading still fails, open the game log, `%USERPROFILE%\AppData\LocalLow\CarX Technologies\CarX Drift Racing Online 2\Player.log`, and find `Failed to load mod '<MapName>'`. The exception that follows names the reason. <!-- src: DR3PC1:Assets/DR3/Scripts/States/Controllers/Mods/ModRoomsHelper.cs:322 -->

See also: [Load the map in the game](test-in-game.md#in-game)

### Other players can't join a room on your local map

**Cause.** A room on a local map is open only to players who have a map folder with the same name in their own `Mods` folder. Other players don't see the room in their room list, and joining it by code shows `Mod Not Installed` with `Install the track mod to join this room.` A map published on mod.io downloads when a player joins, in game builds that support mod.io. <!-- src: DR3PC1:Assets/DR3/Scripts/States/Controllers/Mods/ModRoomsHelper.cs:113-116 -->

**Fix.**

- To test with friends, give them a copy of your exported map folder. They put it in their `<game folder>/Mods/` under the same folder name.
- To share the map with everyone, publish it to mod.io.

See also: [Test the map in the game](test-in-game.md) · [Publish to mod.io](publish.md)

### `The room uses a different version of this track mod. Update the…`

**Cause.** Every **Update publication** uploads a new file, and a room on a mod.io map accepts only players who have the same version installed as the host. <!-- src: DR3PC1:Assets/DR3/Scripts/States/Controllers/Mods/ModRoomsHelper.cs:449-455 -->

**Fix.**

- Wait until the game has updated the map from mod.io for the host and for the players, then create or join the room again.

See also: [Update a publication](publish.md#update)

## Upload problems

### `'<scene>': the map is <X> mb and this vendor accepts 4096 mb.`

Severity: Error (stops the upload) · Shown in: Console <!-- src: Assets/Editor/MapBuilder.Storage.cs:169-170 -->

**Cause.** Right before **Create publication** and **Update publication**, MapBuilder measures the build cache: the map part may be at most 4096 MB and the metadata part at most 24 MB. The metadata check gives `'<scene>': the meta is <X> mb and this vendor accepts 24 mb.`; the metadata holds the Icon, the Preview and the minimap images. **Validate map** and the **Export to folder** button don't check these sizes. <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:25-26 -->

The footer then shows `Publication did not complete. See the Console.` or `Update did not complete. See the Console.`

**Fix.**

1. For the map part, reduce texture sizes and remove geometry that the map doesn't need. For the metadata part, use smaller Icon, Preview and minimap images.
1. Select the **Build map** button, then publish again.

See also: [Publish to mod.io](publish.md) · [Where the build goes](build.md#cache)

### The publications list is empty although you have publications

**Cause.** MapBuilder couldn't read your publications from mod.io, and the Console shows `Could not read your mod.io creations: <reason>`. The list then keeps its previous content, usually `No publications on mod.io yet.` The reason often reads `You have used the input_json parameter with semantically incorrect JSON.`, which doesn't describe the real problem. Two causes are known: <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoPublisher.cs:126 -->

- A server or session error, for example a session from an older mod.io address. The lines just above the error in the Console then show `HTTP Code: [502]`.
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

If the list still fails after you sign in again, and mod.io shows a publication without a file on your account, delete that publication by its ID. **Delete publication** asks for the ID when no row is selected, so it works without the list.

> [!CAUTION]
> Deleting removes the publication and every file published to it from mod.io. It can't be undone.

To delete a publication without a file:

1. On mod.io, open the page of the publication without a file and copy its ID.
1. If a row in **Publications** is selected, in **LOCAL MAPS** select a map that shows `mod.io: not linked`.
   No row is selected for a map without a linked publication.
1. Select **Delete publication**.
   The `Delete from mod.io` dialog asks for **Item id**. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/Publishing/ModPublisherSession.cs:148-153 -->
1. In **Item id**, enter the ID, and select **Continue**.
1. In the confirmation dialog, check the ID, and select **Delete**.
1. Select **Refresh list**.

See also: [Publish to mod.io](publish.md) · [Sign in to mod.io](publish.md#sign-in)

### `mod.io sign in failed: …`

Severity: Error (you stay signed out) · Shown in: Console <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoAuthProvider.cs:103 -->

**Cause.** mod.io rejected the code, for example because it expired, or the connection failed. Two related messages mean that you stopped the sign-in yourself: `The mod.io terms of use were not accepted.` after **Decline**, and `Sign in cancelled.` after you cancel the email prompt.

**Fix.**

1. In the header of the **MapBuilder** window, select **Sign in**.
1. If the `mod.io terms of use` dialog opens, select **Accept**.
1. In **Email**, enter the email address of your mod.io account, and select **Continue**.
1. In **Code**, enter the code from the newest email from mod.io, and select **Continue**.

See also: [Sign in to mod.io](publish.md#sign-in)

### The header shows `Unavailable`

**Cause.** mod.io couldn't start, usually because there is no connection. The box at the top of the **Publish** tab shows the reason, for example `mod.io failed to initialize: <reason>`. **Validate map**, the **Build map** button and the **Export to folder** button don't need mod.io. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoEditorBootstrap.cs:132 -->

**Fix.**

1. Check your internet connection.
1. Close the **MapBuilder** window and open it again with **Tools** > **MapBuilder**. The window connects to mod.io each time it opens.

See also: [Sign in to mod.io](publish.md#sign-in)

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

### The submodule folder is empty and the project doesn't compile

**Cause.** `Assets/Plugins/CarX.Modding.Creator` is a git submodule. **Code** > **Download ZIP** on GitHub doesn't include it, and neither does a clone made without `--recurse-submodules`.

**Fix.**

- For a ZIP download, clone the project with git instead:

  ```bash
  git clone --recurse-submodules -b feature/2.0/3.0 https://github.com/CarXTechnologies/dro-map-uploader
  ```

- For a clone without the submodule, run `git submodule update --init --recursive` in the project folder.

Both commands fetch the submodule from its SSH address. If a command reports `git@github.com: Permission denied (publickey).`, run the HTTPS command from [that entry](#ssh-publickey) instead. <!-- src: .gitmodules:3 -->

See also: [Get the project](getting-started.md#clone) · [Update an existing clone](getting-started.md#update-clone)

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

**Cause.** **Scene** lists only the scenes that are enabled in the build scene list, and the list has none. MapBuilder then keeps checking and building the scene of the last build, or the scene that was open when you selected the map, which may not be the one you expect. If that scene was never saved, validation reports `No target scene is selected. Pick one in Build Settings.`; you choose the scene in **Scene** on the **Build** tab. <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:780-786; Assets/Editor/MapBuilderEditorWindow.Workspace.cs:236-237 --> A scene under `Assets/MapResources` is added and enabled whenever the editor loads or you save the scene, and a scene you disable in the list is dropped from it at the next save. <!-- src: Assets/Editor/MapBuilderProcessor.cs:79 -->

**Fix.**

1. Move the scene into `Assets/MapResources/<MapName>/`.
1. Open the scene and save it.
1. In the **MapBuilder** window, on the **Build** tab, select the scene in **Scene**.

A scene outside `Assets/MapResources` has to be added and enabled by hand: select **File** > **Build Profiles**, then, in the **Build Profiles** window, select **Scene List** and add the scene.

See also: [Create the map folder and scene](scene-setup.md#scene)

<a name="faq"></a>
## Common questions

### Can I use Unity Terrain?

Not directly. Terrain and TerrainCollider aren't exported, so the car falls through them in the game. Model the ground as a mesh with a MeshCollider instead; see [Import models](scene-setup.md#import).

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

**Create publication** creates the publication hidden, and **Update publication** never changes its visibility. Change the visibility on the publication's page on mod.io; the tooltip of the size line in its **Publications** row gives the page address. See [Create a publication](publish.md#create).

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
