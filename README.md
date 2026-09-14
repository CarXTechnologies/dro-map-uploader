# 🗺 Uploading tracks

*[Русская версия](README.ru.md)*

Step-by-step guide to preparing a map in the Unity project and publishing it.

The mod data format is independent of a particular game. Binary packages use `mod.cxmod`; Wavefront is the alternative loose-file representation. Currently the only content type is `map`. A consuming game needs a compatible loader and adapters for rendering, physics and gameplay markers. Selecting a publishing platform does not select a file format.

The current MapBuilder UI publishes to **mod.io** or exports to a local folder. Publisher setup is described in **[PUBLISHING.md](PUBLISHING.md)**.

- [Preparing the track upload project](#preparing-the-track-upload-project)
- [Importing the 3D model into the project](#importing-the-3d-model-into-the-project)
- [Adding core components](#adding-core-components)
  - [Assigning surface collisions](#assigning-surface-collisions)
  - [Assigning a spawn point on the map](#assigning-a-spawn-point-on-the-map)
  - [Template system (road only)](#template-system-road-only)
  - [Adding a mini-map](#adding-a-mini-map)
  - [Capturing prototypes: icon, preview, minimap](#capturing-prototypes-icon-preview-minimap)
- [Publishing a map](#publishing-a-map)
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

   <img src="Image/2.png" alt="Material setup" style="width:400px;"/>

5. Open the created scene and drag the 3D model onto it to create a GameObject.
6. Add the required components to the created GameObject.
7. To make the object reusable (Prefab), create the folder `Assets/MapResources/<your_folder>/Prefabs`.
8. Right-click the object in the scene and choose **Prefab → Unpack Completely**.
9. Drag the GameObject from the scene into the new `Prefabs` folder — it can now be reused as many times as needed.

   <img src="Image/3.png" alt="Prefab folder" style="width:300px;"/>

The scene may contain any number of top-level objects. A shared parent named `root` is optional: the builder exports all scene roots without reparenting the source objects. Existing maps with a `root` group remain supported. The builder does not create temporary scenes. Rigidbody and Animation bindings and animation atlas deduplication span the whole exported scene.

## Adding core components

The project supports several component types that are ported into the game. The main ones are:

- the point where the car appears on the map,
- physical materials of surfaces.

These components are assigned with the **GameMarkerData** helper. To add it to a GameObject or Prefab, click **Add Component** in the Inspector and type `GameMarkerData`. A mini-map can be added as well.

### Assigning surface collisions

For the track object that represents the surface, set the GameMarkerData type to **Road** and pick, in the dropdown, the material type used in the game when interacting with this surface.

<img src="Image/4.png" alt="Road marker setup" style="width:500px;"/>

> [!NOTE]
> Any GameObject/Prefab with collision must also have a Collider component (Box / Sphere / Capsule / Mesh Collider). This is required for collision accuracy.

### Assigning a spawn point on the map

Create an empty object via **GameObject → Create Empty** (or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd>). In the Transform component, set the coordinates where the car should appear in the game. Add the **GameMarkerData** component and choose the **SpawnPoint** type.

> [!IMPORTANT]
> Both formats support several spawn points — name each object meaningfully,
> because the name is exported with the spawn point and identifies it in game.

<img src="Image/5.png" alt="Spawn point setup" style="width:400px;"/>

### Vertex animation

Use **Animation** with an Animator to bake animation into an atlas. **Ambient**, **TimeObjectActivator** and **NetworkObject** are unsupported. Network physics uses Rigidbody without a separate marker.

### Template system (road only)

1. Create a template config.

   <img src="Image/22.png" alt="Creating a template config" style="width:500px;"/>

2. Create and redefine the template parameters.

   <img src="Image/23.png" alt="Template parameters" style="width:350px;"/>

3. Select the template config in the GameMarkerData component.

   <img src="Image/24.png" alt="Selecting the template config" style="width:500px;"/>

4. Select a template to reassign the parameters.

   <img src="Image/25.png" alt="Selecting a template" style="width:400px;"/>

### Adding a mini-map

Every map needs exactly one minimap. Create an empty object in the scene (as described above), add the **Minimap** component and select the Minimap Layer. In **Textures → Element 0**, assign at least one texture — MainTexture. While configuring the map you can use the auxiliary functions to generate a template, load it into a graphics editor and design your own minimap on top of it.

- **Bound center** — the minimap's offset relative to the center.
- **Bound size** — the map's size in world scale.

> [!NOTE]
> The map must be centered relative to zero coordinates.

<img src="Image/8.png" alt="Minimap setup" style="width:800px;"/>

### Capturing prototypes: icon, preview, minimap

1. Add the **CaptureCamera** component to a Camera GameObject.
2. Set up the camera for your prototype.
3. Press **Capture** at the bottom of the component in the Inspector.
4. Save the prototype to disk.

<img src="Image/14.png" alt="Capture camera" style="width:300px;"/>

## Publishing a map

1. Open **Tools → MapBuilder**. The header shows your account, **Platform** (currently mod.io) and the **EN / RU** language selector. English is the default.
2. Select a map in **Local maps**, or press **+ New map** to create its settings asset. Existing `MapMetaConfig` assets appear in the library automatically; there is no separate config selector on each tab.
3. On **Map**, fill in **Name**, **Version**, **Summary**, **Description**, **Authors**, **URL**, **Preview** and **Icon**. Version is the author's release label (for example `1.2.0`), not the container format version; an empty value uses `1.0.0`. An empty Summary uses the first description line. Use readable PNG images for Icon and Preview.

![Map tab: metadata and images](Image/current/map-guide.png)

### Build Settings

**Build → Optimization** provides sector splitting/merging, optional collider simplification and the **Generate sector LODs** checkbox. See [Build optimization](BuildOptimization.md) for settings and exclusions.

Open **Build**. Add the target scene to Unity's build scene list if it does not appear in **Scene**.

| Control | Purpose |
| --- | --- |
| **Scene** | Scene to export. |
| **Format** | Binary (default) or Wavefront. |
| **Advanced settings** | Opens **Rebuild targets** and, for Binary, **Binary textures** (BC7 or RGBA32 with prepared mipmaps). Container compression is automatic. |
| **Scene contents** | Shows component counts and limits. |
| **Validate map** | Checks the map without building. See [Validation](#validation). |
| **Build map** | Builds the selected targets. Use Everything for a complete build. |
| **Open build folder** | Opens the generated output. |
| **Go to publishing →** | Opens Publish. |

Build progress and **Cancel** appear in the bottom bar. Some Unity export stages still run on the editor thread, so responsiveness can vary during a build.

![Build tab](Image/current/build-guide.png)

### Mod format: Wavefront and Binary

- **Wavefront** exports geometry/materials/textures/metadata as OBJ / MTL / PNG / JSON files.
- **Binary** packages the map into compressed `mod.cxmod`, with compact geometry, typed scene data and prepared textures. This is the default format.

Both export all scene roots directly and support the same scene features. Rebuild **Map and Meta** after switching formats. Only maps (`contentType: "map"`) are supported. The format is independent of a particular game; the consuming application needs compatible adapters.

See [Wavefront export](Wavefront.md) and [Binary format](BinaryFormat.md).

### Upload Settings

On **Publish**, choose **Platform** to publish to mod.io, or **Export to folder** for a local copy. Building and exporting locally do not require a publication.

1. Build Map and Meta first, then sign in from the header to publish.
2. For a new mod, press **Create publication**. It creates the entry and uploads the built files. After the entry is linked, rebuild Meta to include its assigned id and press **Update publication**.
3. For an existing mod, select it under **Publications**, enter **Changelog**, choose **Update title / Update description / Update preview**, and press **Update publication**. These switches control changes to the publication page.
4. Use **Refresh list** to reload publications. The local map list shows the linked mod.io file version when loaded; this is separate from the locally edited Version.

![Publish tab: mod.io publication controls](Image/current/publish-guide.png)

For local testing, select **Export to folder**, choose **Folder**, then press **Export to folder**. Use the mod directory expected by the consuming application. The current UI has no Steam Local Test option.

![Publish tab: export to a folder](Image/current/export-guide.png)

Publisher configuration and integration details: [PUBLISHING.md](PUBLISHING.md).

## Validation

Press **Validate map** on the **Build** tab to check the map without building it. Nothing in the scene is
modified, so it can be run as often as you like — it is the fastest way to find out whether a map is ready.

The same checks run automatically as part of a build. Either way the result opens in a **Map Validation** window
listing everything that was found in one pass, each row with a **Select** button that takes you to the object
responsible. The console only gets a one line summary — **To console** in the window logs the full list when you
want it there, and **Copy** puts it on the clipboard.

- **Errors** stop the build. Fix them all — they are shown together on purpose, so you do not discover them one
  rebuild at a time.
- **Warnings** do not stop anything, and describe skipped content or other issues worth reviewing.

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

Volume and ReflectionProbe are optional preview components. Unsupported components (including UI, particles, video and joints) and unsupported marker types are skipped with warnings, not build-blocking errors. A Rigidbody without usable colliders is skipped; its render geometry can still be exported. Rules and budgets live in `Assets/Editor/MapSceneRules.cs`.

## Requirements

- Avoid using multiple Directional Light sources.
- Keep the map size under 4 GB.
- Keep the meta size under 24 MB (including preview, icon, description and title).
- Be mindful of the component limitations.

The current mod.io configuration sets these limits; project administrators can change them in the publisher config:

| Field | Limit |
| --- | --- |
| Icon / logo | 8 MB |
| Name | 50 characters |
| Description | 50000 characters |
| Summary | 250 characters |

A non-convex MeshCollider on a non-kinematic Rigidbody is invalid and must be fixed. Unsupported components alone do not block a build.

New publications are created hidden. Review the uploaded file on mod.io and change visibility on its page when ready.
