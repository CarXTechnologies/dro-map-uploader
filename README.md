# 🗺 Uploading tracks

Step-by-step guide to preparing a track in the Unity project and publishing it.

Tracks can be published to the **Steam Workshop** or to **mod.io**; the vendor is picked at the top of the MapBuilder
window. Setting up the vendors — SDKs, credentials, sign in — is covered in **[PUBLISHING.md](PUBLISHING.md)**.

- [Preparing the track upload project](#preparing-the-track-upload-project)
- [Importing the 3D model into the project](#importing-the-3d-model-into-the-project)
- [Adding core components](#adding-core-components)
  - [Assigning surface collisions](#assigning-surface-collisions)
  - [Assigning a spawn point on the map](#assigning-a-spawn-point-on-the-map)
  - [Assigning ambient sounds](#assigning-ambient-sounds)
  - [Template system (road only)](#template-system-road-only)
  - [Adding a mini-map](#adding-a-mini-map)
  - [Capturing prototypes: icon, preview, minimap](#capturing-prototypes-icon-preview-minimap)
- [Uploading the track to the Workshop](#uploading-the-track-to-the-workshop)
  - [Build Settings](#build-settings)
  - [Mod format: dro1 vs dro2](#mod-format-dro1-vs-dro2)
  - [Upload Settings](#upload-settings)
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

2. Install **Unity Editor 2023.2.20f1** (64-bit only): **[download installer](https://download.unity3d.com/download_unity/0e25a174756c/Windows64EditorInstaller/UnitySetup64-2023.2.20f1.exe)**.
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

## Adding core components

The project supports several component types that are ported into the game. The main ones are:

- the point where the car appears on the map,
- ambient sounds,
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
> Only one **vehicle spawn point** may be placed on the map.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/5.png?raw=true" alt="Spawn point setup" style="width:400px;"/>

### Assigning ambient sounds

Create an empty object via **GameObject → Create Empty** (or <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd>). Add the **GameMarkerData** component, select the **Ambient** type, and then pick the sound type that best suits the map in the dropdown.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/6.png?raw=true" alt="Ambient marker setup" style="width:400px;"/>

With an Ambient marker you can also use the **DrawZoneBehaviour** component — a helper that draws the zone where the assigned sounds will be heard.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/7.png?raw=true" alt="Ambient zone gizmo" style="width:300px;"/>

If you need another helper script, you can write your own and add it to `Assets/Resources/MapSkipComponent`.

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

The minimap is optional. Create an empty object in the scene (as described above), add the **Minimap** component and select the Minimap Layer. In **Textures → Element 0**, assign at least one texture — MainTexture. While configuring the map you can use the auxiliary functions to generate a template, load it into a graphics editor and design your own minimap on top of it.

- **Bound center** — the minimap's offset relative to the center.
- **Bound size** — the map's size in world scale.

> [!NOTE]
> The map must be centered relative to zero coordinates.

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/8.png?raw=true" alt="Minimap setup" style="width:800px;"/>

### Capturing prototypes: icon, preview, minimap

1. Add the **CaptureCamera** component to a Camera GameObject.
2. Set up the camera for your prototype.
3. Open the component context menu and press **Capture**.
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

   - **Workshop Name** — the map name shown in the Workshop.
   - **Workshop Description** *(optional)* — the description shown in the Workshop.
   - **Icon** — the map icon shown in the list of Workshop maps in the game (Read/Write enabled required, PNG only).
   - **Preview** — the map preview shown in the Workshop and when entering the map in the game (Read/Write enabled required, PNG only).

   ![Map meta config](https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/16.png?raw=true)

5. To set up a scene for the build, select the **MapMetaConfig** in the MapBuilder window.

   <img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/27.png?raw=true" alt="Selecting MapMetaConfig" style="width:600px;"/>

### Build Settings

| Setting | Description |
| --- | --- |
| **Target Scene** | The scene used to build the map bundle (the scene must be in Build Settings). |
| **Format** | The mod packaging format: **dro1** or **dro2** — see [Mod format: dro1 vs dro2](#mod-format-dro1-vs-dro2). |
| **Compression** | Shown and used for **dro1** builds only. |
| **Build Targets** (flags) | The build targets you want to build or rebuild. |
| **Build** | Builds all selected Build Targets. |

<img src="https://github.com/CarXTechnologies/dro-map-uploader/blob/target/1.1/Image/20.png?raw=true" alt="Build settings" style="width:400px;"/>

### Mod format: dro1 vs dro2

- **dro1** — the original format. The scene and its meta data are packed into Unity **AssetBundles**, with an optional Compression setting.
- **dro2** — the newer format. The map is exported into a plain-data catalog (obj / mtl / png / json) instead of AssetBundles, so it is not tied to the Unity version it was built with. The Compression setting is unused and hidden when dro2 is selected.

📄 Full dro2 documentation: **[DRO2.md](DRO2.md)** — output structure, mesh/material/texture export rules, LOD and marker handling, limits and troubleshooting.

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

## Supported components

| Category | Components |
| --- | --- |
| **Physics** | MeshCollider, BoxCollider, SphereCollider, CapsuleCollider, Rigidbody |
| **Graphics** | ReflectionProbe, Volume |
| **Renderer** | MeshRenderer, MeshFilter, Light, LODGroup, ParticleSystemRenderer, VFX particle |
| **UI** | Canvas, RawImage, TextMeshProUGUI |
| **Other** | VideoPlayer (1280×720, 30 fps, 15 sec) |

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
