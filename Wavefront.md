# Uploading mods in the **Wavefront** format

This document describes the **Wavefront** representation supported by MapUploader. It is not a game version. The primary binary representation is documented in [Binary format](BinaryFormat.md). Both require a compatible consuming application; game-specific marker behavior belongs to its integration layer.
For the general map authoring workflow (scene setup, markers, minimap, workshop publishing) see the main **[README](README.md)** — everything described there still applies. This file only covers what is *specific* to Wavefront.

- [What is Wavefront](#what-is-wavefront)
- [Supported content](#supported-content)
- [Selecting the format](#selecting-the-format)
- [Publishing](#publishing)

## What is Wavefront

**Wavefront** exports the map into a **plain-data catalog**:

- geometry as `.obj` files,
- materials as `.mtl` files,
- textures as `.png` files,
- scene structure and gameplay data as `.json` files.

Because nothing in the output depends on Unity's serialization, a Wavefront mod is not tied to the Editor version it was built with, and its content can be inspected and diffed as regular files on disk.

Wavefront and Binary share the same scene authoring rules, component whitelist and `MapMetaConfig`.

## Supported content

This is the **complete** list of what the new format currently exports. Unsupported runtime components are skipped and reported without blocking the build. Invalid data in supported components can still block export. Optional preview components are reported as information.

### ECS static geometry

The whole map — trees, props, roads, buildings — is exported as ECS static instances. There are no special components or prefab types: an object is picked up if it has

- `MeshFilter` + `MeshRenderer` — geometry and materials,
- `MeshCollider` — collision.

Identical mesh + material + collider combinations are stored once and reused by every instance, so repeated props cost only their transform. Objects without any of these components are not exported.

### LODs

`LODGroup` is supported, up to **8 LOD levels** per group. Screen-relative transition heights are converted into world distances on export. A group with more than 8 levels is skipped with a warning; a group that ends up with one usable level becomes a plain static instance.

### Spawn points

`SpawnPoint` markers (`GameMarkerData`) define where cars appear. The map supports **multiple** spawn points per map, and the **name of the spawn point GameObject** is exported together with its transform — name the objects meaningfully, the name travels with the mod and identifies the spawn point in game.

### Minimap

Exactly **one** `Minimap` component per map. Its textures are exported as PNG and referenced from the mod meta together with the bounds center and size.

### Surface types

`Road` markers (`GameMarkerData`) define the physical surface of the road mesh. Supported surface types:

`Asphalt`, `Grass`, `Sand`, `Earth`, `Snow`, `Ice`, `Gravel`.

The marker also carries the friction and bump parameters of the selected surface template.

### What is not exported

Preview environment components are optional and are reported as information. Other unsupported components are reported during [validation](README.md#validation).

| Category | Not exported by Wavefront |
| --- | --- |
| **Physics** | FixedJoint, SpringJoint, HingeJoint |
| **Graphics** | ReflectionProbe, Volume (and their HDRP data components) |
| **Renderer** | Directional and Area lights; Animator is baked only for Animation markers |
| **UI** | Canvas, CanvasScaler, CanvasRenderer, RectTransform, TextMeshProUGUI, RawImage |
| **Particles** | ParticleSystem, ParticleSystemRenderer, VisualEffect (VFX Graph) |
| **Other** | VideoPlayer |

MeshCollider, BoxCollider, SphereCollider, CapsuleCollider and Rigidbody are exported. Vertex animations use baked atlases; see the Creator documentation.

Also dropped, with a warning at export time:

- a `LODGroup` with more than 8 levels — the whole group, including its geometry,
- a material texture that is not a `Texture2D`,
- an object with no `MeshFilter` + `MeshRenderer` and no `MeshCollider`.

## Selecting the format

1. Open **Tools → MapBuilder** and select a local map, or use **+ New map**.
2. Fill in its metadata on **Map**.
3. Open **Build**, select **Scene** and choose **Wavefront** under **Format**. The scene must be in Unity's build scene list.
4. Under **Advanced settings**, select **Rebuild targets → Everything** for a complete build. Binary texture settings are not shown for Wavefront.
5. Press **Validate map**, then **Build map**. Progress and Cancel appear in the bottom bar.

## Publishing

- **Publish → Export to folder** — copies the build to any folder on disk. Useful for inspecting the catalog or for manual distribution.
- **Publish → Platform** — uploads the catalog to mod.io. A Wavefront catalog is validated and uploaded as a set of data files. See **[PUBLISHING.md](PUBLISHING.md)**.

> Note! Whether the shipped game reads a Wavefront mod delivered through a vendor depends on the game build you are testing
> against. Loading from a local folder is the path that is known to work:
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
