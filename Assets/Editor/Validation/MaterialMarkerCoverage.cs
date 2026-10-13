using System.Collections.Generic;
using System.Linq;
using Plugins.CarX.Modding.Creator.Editor;
using Plugins.CarX.Modding.Creator.Runtime;
using UnityEngine;

namespace Editor.Validation
{
	/// <summary>
	/// Which Material marker applies to each exported MeshRenderer of a Binary build, by the exporter's rules (SceneFormatCollector and
	/// <see cref="RenderCoreMaterialOverrides.Resolve"/>): the marker on the renderer's own object, or, for a renderer assigned to a LOD of
	/// a LODGroup and without a marker of its own, the marker on the LODGroup object.
	/// </summary>
	internal static class MaterialMarkerCoverage
	{
		/// <summary>The exporter skips a LODGroup with more levels, and its renderers are exported as standalone objects.</summary>
		private const int MaxLodLevels = 8;

		/// <summary>
		/// Exported MeshRenderers with a mesh, each with the object and settings of the Material marker that applies to it (both null without
		/// one). Like the exporter, it keeps inactive objects and disabled renderers (they are exported too) and skips renderers inside a baked
		/// vertex animation (they keep their own materials) and standalone objects that are inactive under a Rigidbody.
		/// <paramref name="transforms"/> are the validated transforms (Garbage and engine-owned objects already left out).
		/// </summary>
		public static List<(MeshRenderer renderer, GameObject markerObject, RenderCoreMaterialMarkerSettings settings)> Collect(
			IReadOnlyList<Transform> transforms, IReadOnlyList<Transform> animationRoots)
		{
			Dictionary<MeshRenderer, Transform> lodRoots = CollectLodRoots(transforms, animationRoots);
			var result = new List<(MeshRenderer renderer, GameObject markerObject, RenderCoreMaterialMarkerSettings settings)>();

			foreach (Transform transform in transforms)
			{
				MeshRenderer renderer = transform.GetComponent<MeshRenderer>();
				MeshFilter filter = transform.GetComponent<MeshFilter>();

				if (renderer == null || filter == null || filter.sharedMesh == null || IsAnimated(transform, animationRoots))
				{
					continue;
				}

				bool inLod = lodRoots.TryGetValue(renderer, out Transform lodRoot);

				// The exporter skips inactive objects under a Rigidbody only on the standalone path, not as LODs of a LODGroup.
				if (!inLod && IsInactiveRigidbodyPart(transform))
				{
					continue;
				}

				GameObject markerObject = transform.gameObject;
				RenderCoreMaterialMarkerSettings settings = RenderCoreMaterialOverrides.FindSettings(markerObject);

				if (settings == null && inLod)
				{
					markerObject = lodRoot.gameObject;
					settings = RenderCoreMaterialOverrides.FindSettings(markerObject);
				}

				result.Add((renderer, settings != null ? markerObject : null, settings));
			}

			return result;
		}

		/// <summary>
		/// LODGroup object of every MeshRenderer assigned to a LOD of an exported LODGroup. A LODGroup inside a baked vertex animation,
		/// inactive under a Rigidbody or with too many levels is not exported as a group, so its renderers get no LODGroup marker.
		/// </summary>
		private static Dictionary<MeshRenderer, Transform> CollectLodRoots(IReadOnlyList<Transform> transforms, IReadOnlyList<Transform> animationRoots)
		{
			var lodRoots = new Dictionary<MeshRenderer, Transform>();

			foreach (Transform transform in transforms)
			{
				LODGroup lodGroup = transform.GetComponent<LODGroup>();

				if (lodGroup == null || lodGroup.lodCount > MaxLodLevels || IsAnimated(transform, animationRoots) || IsInactiveRigidbodyPart(transform))
				{
					continue;
				}

				foreach (LOD lod in lodGroup.GetLODs())
				{
					if (lod.renderers == null)
					{
						continue;
					}

					foreach (Renderer renderer in lod.renderers)
					{
						if (renderer is MeshRenderer meshRenderer && meshRenderer != null && !IsAnimated(meshRenderer.transform, animationRoots))
						{
							lodRoots.TryAdd(meshRenderer, transform);
						}
					}
				}
			}

			return lodRoots;
		}

		private static bool IsAnimated(Transform transform, IReadOnlyList<Transform> animationRoots)
		{
			return animationRoots.Any(root => root != null && transform.IsChildOf(root));
		}

		private static bool IsInactiveRigidbodyPart(Transform transform)
		{
			return !transform.gameObject.activeInHierarchy && transform.GetComponentInParent<Rigidbody>(includeInactive: true) != null;
		}
	}
}
