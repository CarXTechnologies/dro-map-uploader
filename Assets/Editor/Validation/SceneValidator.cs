using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Editor.Validation
{
	public static class SceneValidator
	{
		public const string CategoryComponents = "Components";
		public const string CategoryBudget = "Budget";
		public const string CategoryMarkers = "Markers";
		public const string CategoryFormat = "Format";
		public const string CategoryLighting = "Lighting";
		public const string CategoryGeometry = "Geometry";
		public const string CategoryPhysics = "Physics";
		public const string CategoryMinimap = "Minimap";

		private const string GarbageTag = "Garbage";

		private const HideFlags EngineOwned =
			HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

		public static void Validate(
			MapValidationReport report,
			IReadOnlyList<GameObject> roots,
			FormatBuild format,
			ValidItemData rules,
			IReadOnlyList<string> skipTypes,
			Transform pathRoot = null)
		{
			report.pathRoot = pathRoot;
			report.format = format;

			ResetRules(rules);

			var transforms = CollectTransforms(roots);

			CheckComponents(report, transforms, rules, skipTypes);
			CheckBudget(report, rules);
			CheckMarkers(report, transforms);
			CheckLighting(report, transforms);
			CheckGeometry(report, transforms);
			CheckPhysics(report, transforms);
            CheckSectorBudgets(report, transforms);
			CheckMinimap(report, transforms);

			report.FlushSuppressed();
		}

		private static void ResetRules(ValidItemData rules)
		{
			if (rules.data == null)
			{
				return;
			}

			for (var index = 0; index < rules.data.Count; index++)
			{
				var item = rules.data[index];
				item.Reset();
				item.current = 0;
				rules.data[index] = item;
			}
		}

		private static List<Transform> CollectTransforms(IReadOnlyList<GameObject> roots)
		{
			var all = new List<Transform>();

			if (roots == null)
			{
				return all;
			}

			foreach (var root in roots)
			{
				if (root == null)
				{
					continue;
				}

				foreach (var transform in root.GetComponentsInChildren<Transform>(true))
				{
					if (transform != null && !IsExcluded(transform))
					{
						all.Add(transform);
					}
				}
			}

			return all;
		}

		private static bool IsExcluded(Transform transform)
		{
			for (var current = transform; current != null; current = current.parent)
			{
				if (current.CompareTag(GarbageTag))
				{
					return true;
				}

				if ((current.gameObject.hideFlags & EngineOwned) != HideFlags.None)
				{
					return true;
				}
			}

			return false;
		}

		private static void CheckComponents(MapValidationReport report, List<Transform> transforms, ValidItemData rules,
			IReadOnlyList<string> skipTypes)
		{
            var animationRoots = Plugins.CarX.Modding.Creator.Editor.VertexAnimationBaker.GetAnimationRoots(
                transforms.Select(t => t.root).Distinct(), IsExcluded);
			foreach (var transform in transforms)
			{
				var components = transform.GetComponents<Component>();

				for (var i = 0; i < components.Length; i++)
				{
					var component = components[i];

					if (component == null)
					{
						report.AddCapped(MapValidationSeverity.Info, CategoryComponents, "missing-script",
							$"'{transform.name}' has a missing script in component slot {i}. This slot is skipped during export.",
							transform.gameObject);
						continue;
					}

					if (((component is Animator || component is SkinnedMeshRenderer) &&
					     animationRoots.Any(root => transform.IsChildOf(root))) ||
					    MapSceneRules.CountExportedComponent(component, rules.data))
					{
						continue;
					}

					var typeName = component.GetType().Name;

					if (IsPreviewEnvironment(typeName))
					{
						report.AddCapped(MapValidationSeverity.Info, CategoryFormat, "preview-" + typeName,
							$"'{typeName}' is used only for the SDK preview and is not exported or required.", component);
						continue;
					}
					if (typeName == "SceneObjectIDMapSceneAsset") continue;

					if (component is Animator || component is SkinnedMeshRenderer)
					{
						report.AddCapped(MapValidationSeverity.Info, CategoryComponents, "unbaked-animation",
							$"'{typeName}' on '{transform.name}' requires a GameMarkerData Animation marker on the Animator root. " +
							"This component is skipped; add the marker to export baked vertex animation.", component);
						continue;
					}

					if (skipTypes != null && skipTypes.Contains(typeName))
					{
						continue;
					}

					report.AddCapped(MapValidationSeverity.Info, CategoryComponents, "unsupported-" + typeName,
						$"'{typeName}' on '{transform.name}' is not a supported component and will not be exported. " +
						"It is skipped automatically; supported components on this object are still exported.",
						component);
				}
			}
		}

		private static bool IsPreviewEnvironment(string type) =>
			type == "Volume" || type == nameof(ReflectionProbe) || type == "HDAdditionalReflectionData";

		private static void CheckBudget(MapValidationReport report, ValidItemData rules)
		{
			if (rules.data == null)
			{
				return;
			}

			for (var index = 0; index < rules.data.Count; index++)
			{
				var item = rules.data[index];

				if (item.current < item.min)
				{
					report.Error(CategoryBudget,
						$"'{item.type}': the map has {item.current}, at least {item.min} is required.",
						item.components != null && item.components.Count > 0 ? item.components[0] : null);
				}

				if (item.current > item.max)
				{
					report.Error(CategoryBudget,
						$"'{item.type}': the map has {item.current}, at most {item.max} is allowed.",
						item.components != null && item.components.Count > 0 ? item.components[0] : null);
				}

			}
		}

		private static void CheckMarkers(MapValidationReport report, List<Transform> transforms)
		{
			var spawnPoints = new List<GameMarkerData>();
			var markers = new List<GameMarkerData>();

			foreach (var transform in transforms)
			{
				var marker = transform.GetComponent<GameMarkerData>();

				if (marker == null)
				{
					continue;
				}

				markers.Add(marker);

				var head = Head(marker);

				if (string.IsNullOrEmpty(head))
				{
					report.AddCapped(MapValidationSeverity.Error, CategoryMarkers, "marker-no-type",
						$"'{transform.name}' has a GameMarkerData with no type selected.", marker);
					continue;
				}

				if (!MarkerData.IsSupportedHead(marker.markerData.head))
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "unsupported-marker-" + head,
						$"'{transform.name}' uses unsupported marker '{marker.markerData.head}'. " +
						"This marker will be skipped during export. Supported types are SpawnPoint, Road and Animation.", marker);
					continue;
				}

				if (head == "spawnpoint")
				{
					spawnPoints.Add(marker);
				}

				if (head == "animation")
				{
					var settings = marker.markerData.value as Plugins.CarX.Modding.Creator.Runtime.AnimationMarkerSettings;
					var animator = settings?.animator != null ? settings.animator : marker.GetComponent<Animator>();
					if (animator == null || animator.runtimeAnimatorController == null || animator.runtimeAnimatorController.animationClips.Length == 0)
						report.Error(CategoryMarkers, "Animation marker requires an Animator with animation clips.", marker);
				}

				if (head == "road" && marker.GetComponentInChildren<Collider>(true) == null)
				{
					report.AddCapped(MapValidationSeverity.Error, CategoryMarkers, "road-no-collider",
						$"'{transform.name}' is marked as Road but has no Collider on it or its children, so its " +
						"surface type will never be used. Add a Mesh/Box/Sphere/Capsule Collider.",
						marker);
				}
			}

			if (markers.Count == 0)
			{
				report.Error(CategoryMarkers, "The map has no GameMarkerData at all - at minimum it needs a spawn point.");
			}

			switch (spawnPoints.Count)
			{
				case 0:
					report.Error(CategoryMarkers,
						"The map has no SpawnPoint marker, so there is nowhere for a car to appear. " +
						"Add an empty object with GameMarkerData set to SpawnPoint.");
					break;

				case 1:
					break;

				default:
					CheckSpawnPointNames(report, spawnPoints);

					break;
			}
		}

		private static void CheckSpawnPointNames(MapValidationReport report, List<GameMarkerData> spawnPoints)
		{
			foreach (var group in spawnPoints.GroupBy(marker => marker.name))
			{
				if (group.Count() > 1)
				{
					report.Warning(CategoryMarkers,
						$"{group.Count()} spawn points are all named '{group.Key}'. Wavefront/Binary exports the object name as " +
						"the spawn point's identity, so give each one a distinct, meaningful name.",
						group.First());
				}
			}
		}

		private static void CheckLighting(MapValidationReport report, List<Transform> transforms)
		{
			var directional = new List<Light>();

			foreach (var transform in transforms)
			{
				var light = transform.GetComponent<Light>();

				if (light == null)
				{
					continue;
				}

				if (light.type == LightType.Directional)
				{
					directional.Add(light);
				}

				if (!light.enabled)
				{
					continue;
				}

				if (light.type != LightType.Point && light.type != LightType.Spot)
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryLighting, "Wavefront/Binary-light-type",
						$"'{transform.name}' is a {light.type} light. Wavefront/Binary exports Point and Spot lights only, so this " +
						"one will not reach the mod.",
						light);
				}
			}

			if (directional.Count > 1)
			{
				report.Warning(CategoryLighting,
					$"The map has {directional.Count} Directional Lights. These affect the SDK preview only; " +
					"the game supplies its own environment lighting.",
					directional[1]);
			}
		}

		private static void CheckGeometry(MapValidationReport report, List<Transform> transforms)
		{
			foreach (var transform in transforms)
			{
				var filter = transform.GetComponent<MeshFilter>();
				var renderer = transform.GetComponent<MeshRenderer>();

				if (renderer != null && filter == null)
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryGeometry, "renderer-no-filter",
						$"'{transform.name}' has a MeshRenderer but no MeshFilter, so it has no geometry to export.",
						renderer);
				}

				if (filter != null && filter.sharedMesh == null)
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryGeometry, "filter-no-mesh",
						$"'{transform.name}' has a MeshFilter with no mesh assigned.", filter);
				}

				if (renderer != null && renderer.sharedMaterials.Any(material => material == null))
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryGeometry, "renderer-no-material",
						$"'{transform.name}' has an empty material slot; it will render as magenta in game.",
						renderer);
				}

				var meshCollider = transform.GetComponent<MeshCollider>();

				if (meshCollider != null && meshCollider.sharedMesh == null)
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryGeometry, "collider-no-mesh",
						$"'{transform.name}' has a MeshCollider with no mesh assigned, so it collides with nothing.",
						meshCollider);
				}

				CheckLodGroup(report, transform);
			}
		}

		private static void CheckLodGroup(MapValidationReport report, Transform transform)
		{
			var lodGroup = transform.GetComponent<LODGroup>();

			if (lodGroup == null)
			{
				return;
			}

			if (lodGroup.lodCount > 8)
			{
				report.AddCapped(MapValidationSeverity.Error,
					CategoryGeometry, "lod-too-many",
					$"'{transform.name}' has {lodGroup.lodCount} LOD levels; at most 8 are supported and the whole " +
					"group is skipped on export. Merge or remove levels.",
					lodGroup);
				return;
			}

			var lods = lodGroup.GetLODs();

			if (lods.Length == 0 || lods.All(lod => lod.renderers == null || lod.renderers.All(r => r == null)))
			{
				report.AddCapped(MapValidationSeverity.Warning, CategoryGeometry, "lod-empty",
					$"'{transform.name}' has a LODGroup with no renderers assigned to any level.", lodGroup);
			}
		}

		private static void CheckPhysics(MapValidationReport report, List<Transform> transforms)
		{
			foreach (var transform in transforms)
			{
				var meshCollider = transform.GetComponent<MeshCollider>();

				if (meshCollider == null || meshCollider.convex)
				{
					continue;
				}

				var body = meshCollider.GetComponentInParent<Rigidbody>();

				if (body != null && !body.isKinematic)
				{
					report.AddCapped(MapValidationSeverity.Error, CategoryPhysics, "concave-dynamic-collider",
						$"'{transform.name}' has a non-convex MeshCollider driven by a non-kinematic Rigidbody, " +
						"which the physics engine no longer supports. Tick Convex, or make the Rigidbody kinematic.",
						meshCollider);
				}
			}
		}

        private static void CheckSectorBudgets(MapValidationReport report, List<Transform> transforms)
        {
            var settings = (MapManagerConfig.instance.mapMetaConfigValue?.optimization ?? new Plugins.CarX.Modding.Creator.Runtime.BuildOptimizationSettings()).Snapshot();
            foreach (var t in transforms)
            {
                if (!Plugins.CarX.Modding.Creator.Editor.SceneExportOptimization.IsEligible(t)) continue;
                void Check(Mesh mesh, Component context, string kind)
                {
                    if (mesh == null) return;
                    long triangles = 0;
                    for (int i = 0; i < mesh.subMeshCount; i++) if (mesh.GetTopology(i) == MeshTopology.Triangles) triangles += mesh.GetIndexCount(i) / 3;
                    var bounds = Plugins.CarX.Modding.Creator.Editor.SceneExportOptimization.WorldBounds(mesh, t.localToWorldMatrix);
                    var size = bounds.size;
                    if (triangles <= settings.maxTriangles && Mathf.Max(size.x, Mathf.Max(size.y, size.z)) <= settings.sectorSize) return;
                    report.AddCapped(MapValidationSeverity.Warning, CategoryGeometry, "sector-" + kind,
                        $"'{t.name}' ({mesh.name}): static {kind}, {triangles:N0} triangles, world bounds {size.x:F1} x {size.y:F1} x {size.z:F1} m. " +
                        $"Exceeds sector budget ({settings.sectorSize:F0} m / {settings.maxTriangles:N0} triangles). Review Build > Optimization; size alone does not prove a performance issue.", context);
                }
                foreach (var collider in t.GetComponents<MeshCollider>())
                    if (collider.enabled && !collider.isTrigger && !collider.convex) Check(collider.sharedMesh, collider, "collider");
                var renderer = t.GetComponent<MeshRenderer>(); var filter = t.GetComponent<MeshFilter>();
                if (renderer != null && renderer.enabled && filter != null) Check(filter.sharedMesh, renderer, "render mesh");
            }
        }

		private static void CheckMinimap(MapValidationReport report, List<Transform> transforms)
		{
			foreach (var transform in transforms)
			{
				var minimap = transform.GetComponent<Minimap>();

				if (minimap == null)
				{
					continue;
				}

				var size = minimap.BoundsSize;

				if (Mathf.Approximately(size.x, 0f) || Mathf.Approximately(size.y, 0f))
				{
					report.Error(CategoryMinimap,
						$"'{transform.name}': Bound Size is {size}. Set it to the size of the map in world units, " +
						"or the minimap collapses to a point in game.",
						minimap);
				}

				if (minimap.Textures == null || minimap.Textures.Length == 0)
				{
					report.Error(CategoryMinimap,
						$"'{transform.name}': no textures assigned. Add at least one entry with a Main Texture.",
						minimap);
					continue;
				}

				for (var i = 0; i < minimap.Textures.Length; i++)
				{
					var pair = minimap.Textures[i];

					if (pair == null || pair.mainTexture == null)
					{
						report.Error(CategoryMinimap,
							$"'{transform.name}': Textures element {i} has no Main Texture assigned.", minimap);
						continue;
					}

					if (pair.mainTexture as Texture2D == null)
					{
						report.Error(CategoryMinimap,
							$"'{transform.name}': Textures element {i} is a {pair.mainTexture.GetType().Name}. " +
							"Only Texture2D can be exported.",
							minimap);
					}
				}
			}
		}

		private static string Head(GameMarkerData marker)
		{
			return marker == null || marker.markerData == null ? string.Empty : marker.markerData.GetHead();
		}

	}
}
