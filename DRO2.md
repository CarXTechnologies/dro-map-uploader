# Uploading mods in the **dro2** format

This document describes the **dro2** mod format supported by the Map Uploader.
For the general map authoring workflow (scene setup, markers, minimap, workshop publishing) see the main **[README](README.md)** — everything described there still applies. This file only covers what is *specific* to dro2.

- [What is dro2](#what-is-dro2)
- [Supported content](#supported-content)
- [Selecting the format](#selecting-the-format)
- [Publishing](#publishing)

## What is dro2

**dro2** is the second-generation mod packaging format used by the uploader. Instead of building Unity **AssetBundles** (which are version-locked to the exact Unity Editor and player build), dro2 exports the map into a **plain-data catalog**:

- geometry as `.obj` files,
- materials as `.mtl` files,
- textures as `.png` files,
- scene structure and gameplay data as `.json` files.

Because nothing in the output depends on Unity's serialization, a dro2 mod is not tied to the Editor version it was built with, and its content can be inspected and diffed as regular files on disk.

Both formats share the same scene authoring rules, the same component whitelist and the same `MapMetaConfig` — you can rebuild an existing dro1 map as dro2 without changing the scene, as long as the material setup is supported;

## Supported content

This is the **complete** list of what the new format currently exports. Anything else in the scene is ignored by the dro2 build.

### ECS static geometry

The whole map — trees, props, roads, buildings — is exported as ECS static instances. There are no special components or prefab types: an object is picked up if it has

- `MeshFilter` + `MeshRenderer` — geometry and materials,
- `MeshCollider` — collision.

Identical mesh + material + collider combinations are stored once and reused by every instance, so repeated props cost only their transform. Objects without any of these components are not exported.

### LODs

`LODGroup` is supported, up to **8 LOD levels** per group. Screen-relative transition heights are converted into world distances on export. A group with more than 8 levels is skipped with a warning; a group that ends up with one usable level becomes a plain static instance.

### Spawn points

`SpawnPoint` markers (`GameMarkerData`) define where cars appear. Unlike dro1, dro2 supports **multiple** spawn points per map, and the **name of the spawn point GameObject** is exported together with its transform — name the objects meaningfully, the name travels with the mod and identifies the spawn point in game.

### Minimap

Exactly **one** `Minimap` component per map. Its textures are exported as PNG and referenced from the mod meta together with the bounds center and size.

### Surface types

`Road` markers (`GameMarkerData`) define the physical surface of the road mesh. Supported surface types:

`Asphalt`, `Grass`, `Sand`, `Earth`, `Snow`, `Ice`, `Gravel`.

The marker also carries the friction and bump parameters of the selected surface template. Road objects are automatically flagged static during the build.

## Selecting the format

1. Open **Tools → MapBuilder**.
2. Select (or create) a workshop item on the right and attach a `MapMetaConfig` to it.
3. In **Build Settings**:
   - **Target Scene** — the scene to build (it must be added to *File → Build Settings*).
   - **Format** — choose **dro2**.
   - **Compression** — the row disappears when dro2 is selected; the setting is not used by this format.
   - **Build Targets** — flags: `Map`, `Meta`, or both.
4. Press **Build**.

## Publishing

- **External Folder** — copies the build to any folder on disk. Useful for inspecting the catalog or for manual distribution.

> Note! For now a dro2 mod can only be launched from a local folder — Steam Workshop publishing is not available for this format yet.
> Export the build into `Mods/<ModName>/` so that the catalog files lie directly inside that folder:
>
> ```
> Mods/
> └── <ModName>/
>     ├── <id>.json
>     ├── hierarchies/
>     ├── prefabs/
>     ├── lods/
>     ├── markers/
>     ├── models/
>     └── textures/
> ```