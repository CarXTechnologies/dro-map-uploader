# Build optimization

Open **Build → Optimization** and enable **Optimize build** for the selected map. Settings are stored in its config. Changing effective settings requires rebuilding geometry. Optimization is enabled by default for new map configs and supports Wavefront and Binary exports. When it is off, the panel and scene validation show a warning: static geometry is exported as-is, without sector chunking, collider simplification or LODs.

| Setting | Behavior |
|---|---|
| Sector size (m) | Spatial grid size in world units; default 64 m. Triangles crossing boundaries are cut, with interpolated vertex attributes. |
| Max triangles per mesh | Additional limit within each sector; default 8,192. |
| Partition static colliders | Splits eligible non-convex, non-trigger MeshColliders. |
| Partition static render meshes | Splits eligible static MeshRenderer geometry. |
| Merge compatible meshes in each sector | Merges render geometry only with the exact same material and compatible renderer settings. Collider groups preserve layer, physics material, cooking options and Road surface marker. Similar material names are insufficient. |
| Simplify colliders | Collision simplification, controlled by triangle ratio (default 0.5) and error tolerance in metres (default 0.01). Enabled by default. |
| Generate sector LODs | Render LOD generation through meshoptimizer. Enabled by default: 2 additional levels, triangle ratio 0.5 per level, error tolerance 0.05 m (multiplied by the level). Set 1–3 additional levels. |

LOD generation requires **Partition static render meshes**. Sector boundary vertices remain locked during simplification. A level may retain more triangles than its target ratio, or be omitted when it cannot be reduced within the constraints. The last LOD stays visible at distance. Render LODs do not create additional physical colliders.

The optimizer works on temporary export copies; it does not rewrite source scene geometry. Objects under Rigidbody, Animator, Animation or authored LODGroup components are excluded. Transparent renderers, property blocks and baked lightmaps are also excluded from automatic render merging. Unsupported collider arrangements retain the existing export path.

Validation reports oversized eligible meshes and colliders as **warnings**. Size alone does not prove a physics bottleneck. Splitting can increase total triangle count and draw calls; compare gameplay profiling before and after rebuilding. Simplification error is the algorithm's estimate, not a guaranteed maximum surface distance. Test road contact and collision seams after enabling collider simplification.

The pinned [meshoptimizer](https://github.com/zeux/meshoptimizer) 1.2 library runs during export only. The supplied native library supports the Windows x64 Unity Editor; it is excluded from player builds. Other editor platforms require a matching native library. Source, license and build instructions are in `Assets/Plugins/CarX.Modding.Creator/ThirdParty~/meshoptimizer`.
