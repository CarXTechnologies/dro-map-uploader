# 🗺 Uploading tracks

*[Русская версия](README.ru.md)*

Step-by-step guide to preparing a map in the Unity project and publishing it.

The mod data format is independent of a particular game. Binary packages use `mod.cxmod`; Wavefront is the alternative loose-file representation. Currently the only content type is `map`. A consuming game needs a compatible loader and adapters for rendering, physics and gameplay markers. Selecting a publishing platform does not select a file format.

Tracks can be published to the **Steam Workshop** or to **mod.io**; the vendor is picked at the top of the MapBuilder
window. Setting up the vendors — SDKs, credentials, sign in — is covered in **[PUBLISHING.md](PUBLISHING.md)**.

- [Preparing the track upload project](#preparing-the-track-upload-project)
- [Importing the 3D model into the project](#importing-the-3d-model-into-the-project)
- [Adding core components](#adding-core-components)
  - [Assigning surface collisions](#assigning-surface-collisions)
  - [Assigning a spawn point on the map](#assigning-a-spawn-point-on-the-map)
  - [Template system (road only)](#template-system-road-only)
  - [Adding a mini-map](#adding-a-mini-map)
  - [Capturing prototypes: icon, preview, minimap](#capturing-prototypes-icon-preview-minimap)
- [Uploading the track to the Workshop](#uploading-the-track-to-the-workshop)
  - [Build Settings](#build-settings)
  - [Mod format: Wavefront and Binary](#mod-format-wavefront-and-binary)
  - [Upload Settings](#upload-settings)
- [Validation](#validation)
- [Supported components](#supported-components)
- [Requirements](#requirements)

## Preparing the track upload project

1. Get the project. The repository contains a **git submodule** (`Assets/Plugins/CarX.Modding.Creator`), so clone it with `git` rather than downloading the ZIP:

   ```bash
   git clone --recurse-submodules https://github.com/CarXTechnologies/dro-map-uploader
   ```

   > [!WARNING]
   > **Code → Download ZIP** does *not* include submodules — `Assets/Plugins/CarX.Modding.Creator` will be an empty folder and the project will not compile.

   <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/1.png?raw=true" alt="Downloading the project archive" style="width:600px;"/>

2. Install **Unity Editor 6000.3.19f1** (64-bit only): **[download installer](https://download.unity3d.com/download_unity/7689f4515d75/Windows64EditorInstaller/UnitySetup64-6000.3.19f1.exe)**.

   > [!IMPORTANT]
   > The version has to match exactly. It is recorded in `ProjectSettings/ProjectVersion.txt`, and opening the
   > project with a different editor upgrades it in place - which is a change you did not ask for and cannot easily
   > undo.
3. Launch Unity, go to **File → Open Project** and select the project folder (it must contain the `Assets` and `Packages` folders).

When the project opens, you can move on to the next step.

### Updating an existing clone

If you already cloned the repository without `--recurse-submodules`, or after pulling changes that move the submodule:

```bash
git submodule update --init --recursive
```

To pull the latest state of everything, including submodules:

```bash
git pull --recurse-submodules
git submodule update --init --recursive
```

To wipe the working copy and re-fetch everything from scratch — **this discards all local changes**, including untracked files:

```bash
git reset --hard
git clean -xffd                     # -f twice also removes untracked submodule folders
git submodule update --init --recursive --force
```

If the submodule fails to fetch over SSH (`git@github.com: Permission denied`), tell git to use HTTPS instead:

```bash
git config --global url."https://github.com/".insteadOf git@github.com:
git submodule update --init --recursive
```

## Importing the 3D model into the project

1. In `Assets/MapResources/`, create a folder named after your map.
2. Inside that folder, create a scene via **Assets → Create → Scene**.
3. Drag & drop your `.fbx` / [`.obj`](https://www.autodesk.com/products/fbx/overview) / [`.dae`](https://www.khronos.org/collada/) model into `Assets/MapResources/<your_folder>/`.
4. If the models come without materials, create them via **Assets → Create → Material** and configure them as shown below.

   <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/2.png?raw=true" alt="Material setup" style="width:400px;"/>

5. Open the created scene and drag the 3D model onto it to create a GameObject.
6. Add the required components to the created GameObject.
7. To make the object reusable (Prefab), create the folder `Assets/MapResources/<your_folder>/Prefabs`.
8. Right-click the object in the scene and choose **Prefab → Unpack Completely**.
9. Drag the GameObject from the scene into the new `Prefabs` folder — it can now be reused as many times as needed.

   <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/3.png?raw=true" alt="Prefab folder" style="width:300px;"/>

The scene may contain any number of top-level objects. A shared parent named `root` is optional: the builder exports all scene roots without reparenting the source objects. Existing maps with a `root` group remain supported. The builder does not create temporary scenes. Rigidbody and Animation bindings and animation atlas deduplication span the whole exported scene.

## Adding core components

The project supports several component types that are ported into the game. The main ones are:

- the point where the car appears on the map,
- physical materials of surfaces.

These components are assigned with the **GameMarkerData** helper. To add it to a GameObject or Prefab, click **Add Component** in the Inspector and type `GameMarkerData`. A mini-map can be added as well.

### Assigning surface collisions

For the track object that represents the surface, set the GameMarkerData type to **Road** and pick, in the dropdown, the material type used in the game when interacting with this surface.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/4.png?raw=true" alt="Road marker setup" style="width:500px;"/>

> [!NOTE]
> Any GameObject/Prefab with collision must also have a Collider component (Box / Sphere / Capsule / Mesh Collider). This is required for collision accuracy.

### Assigning a spawn point on the map

Create an empty object via **GameObject → Create Empty** (or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd>). In the Transform component, set the coordinates where the car should appear in the game. Add the **GameMarkerData** component and choose the **SpawnPoint** type.

> [!IMPORTANT]
> Both formats support several spawn points — name each object meaningfully,
> because the name is exported with the spawn point and identifies it in game.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/5.png?raw=true" alt="Spawn point setup" style="width:400px;"/>

### Vertex animation

Use **Animation** with an Animator to bake animation into an atlas. **Ambient**, **TimeObjectActivator** and **NetworkObject** are unsupported. Network physics uses Rigidbody without a separate marker.

### Template system (road only)

1. Create a template config.

   <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/22.png?raw=true" alt="Creating a template config" style="width:500px;"/>

2. Create and redefine the template parameters.

   <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/23.png?raw=true" alt="Template parameters" style="width:350px;"/>

3. Select the template config in the GameMarkerData component.

   <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/24.png?raw=true" alt="Selecting the template config" style="width:500px;"/>

4. Select a template to reassign the parameters.

   <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/25.png?raw=true" alt="Selecting a template" style="width:400px;"/>

### Adding a mini-map

Every map needs exactly one minimap. Create an empty object in the scene (as described above), add the **Minimap** component and select the Minimap Layer. In **Textures → Element 0**, assign at least one texture — MainTexture. While configuring the map you can use the auxiliary functions to generate a template, load it into a graphics editor and design your own minimap on top of it.

- **Bound center** — the minimap's offset relative to the center.
- **Bound size** — the map's size in world scale.

> [!NOTE]
> The map must be centered relative to zero coordinates.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/8.png?raw=true" alt="Minimap setup" style="width:800px;"/>

### Capturing prototypes: icon, preview, minimap

1. Add the **CaptureCamera** component to a Camera GameObject.
2. Set up the camera for your prototype.
3. Press **Capture** at the bottom of the component in the Inspector.
4. Save the prototype to disk.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/14.png?raw=true" alt="Capture camera" style="width:300px;"/>

## Uploading the track to the Workshop

1. Open the **Tools → MapBuilder** window.

   ![MapBuilder window](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/17.png?raw=true)

2. Create or select a community item.

   <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/18.png?raw=true" alt="Community item selection" style="width:600px;"/>

   > [!IMPORTANT]
   > Add your scenes to the Build Settings, otherwise they will not be visible in MapBuilder.
   >
   > <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/15.png?raw=true" alt="Build settings scene list" style="width:400px;"/>

3. Create a map configuration in the map folder.

   <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/26.png?raw=true" alt="Creating a map config" style="width:600px;"/>

4. Fill in the configuration file:

   - **Workshop Name** — the map name shown in the Workshop. Letters, digits, spaces and `- _ ' . , : ! ? ( ) & + /`.
   - **Summary** *(optional)* — one line shown next to the map in a listing. mod.io requires one and caps it at 250 characters; left empty, the first line of the description is used instead.
   - **Workshop Description** *(optional)* — the description shown in the Workshop.
   - **Icon** — the map icon shown in the list of Workshop maps in the game (Read/Write enabled required, PNG only).
   - **Preview** — the map preview shown in the Workshop and when entering the map in the game (Read/Write enabled required, PNG only).

   ![Map meta config](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/16.png?raw=true)

5. To set up a scene for the build, select the **MapMetaConfig** in the MapBuilder window.

   <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/27.png?raw=true" alt="Selecting MapMetaConfig" style="width:600px;"/>

### Build Settings

| Setting | Description |
| --- | --- |
| **Target Scene** | The scene used to build the map (the scene must be in Build Settings). |
| **Format** | The mod packaging format: **Wavefront** or **Binary** — see [Mod format: Wavefront and Binary](#mod-format-wavefront-and-binary). |
| **Binary textures** | BC7 or RGBA32, including prepared mipmaps. Container compression is automatic. |
| **Build Targets** (flags) | The build targets you want to build or rebuild. |
| **Validate** | Runs every check against the map without building anything. See [Validation](#validation). |
| **Build** | Builds all selected Build Targets. |
| **Cancel** | Appears while an operation is running and stops it. |

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/20.png?raw=true" alt="Build settings" style="width:400px;"/>

### Mod format: Wavefront and Binary

- **Wavefront** exports geometry/materials/textures/metadata as OBJ / MTL / PNG / JSON files.
- **Binary** packages the map into compressed `mod.cxmod`, with compact geometry, typed scene data and prepared textures. This is the default format.

Both export all scene roots directly and support the same scene features. Rebuild **Map and Meta** after switching formats. Only maps (`contentType: "map"`) are supported.

See [Wavefront export](Wavefront.md) and [Binary format](Assets/Plugins/CarX.Modding.Creator/BinaryFormat.md).

### Upload Settings

| Setting | Description |
| --- | --- |
| **Vendor** *(top bar)* | Where the map is published — Steam Workshop or mod.io. See [PUBLISHING.md](PUBLISHING.md). |
| **Upload Description** | If enabled, the description on the mod page is updated. |
| **Upload Name** | If enabled, the map name on the mod page is updated. |
| **Upload Preview** | If enabled, the map icon on the mod page is updated. |
| **Destination → Vendor** | Uploads to the current item if all Build Targets for the selected config succeeded. |
| **Destination → Local Test** | Replaces the build in the vendor's local install folder only. Steam only. |
| **Destination → External Folder** | Copies the build to any folder on disk. |

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/21.png?raw=true" alt="Upload settings" style="width:400px;"/>

## Validation

Press **Validate** in the Build Settings section to check the map without building it. Nothing in the scene is
modified, so it can be run as often as you like — it is the fastest way to find out whether a map is ready.

The same checks run automatically as part of a build. Either way the result opens in a **Map Validation** window
listing everything that was found in one pass, each row with a **Select** button that takes you to the object
responsible. The console only gets a one line summary — **To console** in the window logs the full list when you
want it there, and **Copy** puts it on the clipboard.

- **Errors** stop the build. Fix them all — they are shown together on purpose, so you do not discover them one
  rebuild at a time.
- **Warnings** do not stop anything, but each one means something you authored will not reach players.

What is checked:

| Area | Checks |
| --- | --- |
| Meta | Name present, allowed characters and within the vendor's length limit; description and summary lengths; icon assigned, readable, PNG and within the size limit; preview readable and within 10 MB |
| Components | Everything against the [supported components](#supported-components) list and its per-type budget; missing (deleted) scripts |
| Markers | A spawn point exists; duplicate spawn point names; markers with no type selected; Road markers with no Collider |
| Format | Components not exported by Wavefront/Binary; optional preview components are informational |
| Lighting | More than one Directional Light; light types Wavefront cannot export |
| Geometry | Renderers with no mesh or empty material slots; MeshColliders with no mesh; LOD groups over 8 levels or with no renderers |
| Physics | Non-convex MeshCollider driven by a non-kinematic Rigidbody |
| Minimap | Bound Size left at zero; missing or non-Texture2D minimap textures |

Objects tagged **Garbage** are skipped, exactly as they are by the build.

## Supported components

| Category | Components |
| --- | --- |
| **Physics** | MeshCollider, BoxCollider, SphereCollider, CapsuleCollider, Rigidbody |
| **Geometry** | MeshRenderer, MeshFilter, LODGroup |
| **Lighting** | Point and Spot Light, HDAdditionalLightData |
| **Animation** | Animator and SkinnedMeshRenderer under GameMarkerData Animation, baked to VAT |
| **Map data** | SpawnPoint, Road, Animation; Minimap |

Volume and ReflectionProbe are optional preview components. UI, particles, video and joints are not exported and produce validation errors. Rules and budgets live in `Assets/Editor/MapSceneRules.cs`.

## Requirements

- Avoid using multiple Directional Light sources.
- Keep the map size under 4 GB.
- Keep the meta size under 24 MB (including preview, icon, description and title).
- Be mindful of the component limitations.

The remaining limits depend on the vendor you publish to, and the uploader validates against whichever one is
selected:

| Limit | Steam Workshop | mod.io |
| --- | --- | --- |
| Preview / logo | 1 MB | 8 MB |
| Map name | 128 characters | 50 characters |
| Description | 8000 characters | 50000 characters |
| Summary | not used | required, 250 characters |

- A non-convex MeshCollider with a non-kinematic Rigidbody is no longer supported.

If the map is configured incorrectly, an error is shown during upload — the listed causes have to be fixed on your side.

Once these steps are complete, the map is published to the vendor you selected. A freshly uploaded map is **private / hidden** on both vendors, so you can test it while it stays visible only to you — on Steam, open **Workshop → Track Workshop** in the game. You can switch it to public on the map page on the vendor site.

> [!WARNING]
> On Steam, the **Friends Only** visibility option currently has issues caused by the external library used for Steam API integration. We plan to fix this in an upcoming release.
