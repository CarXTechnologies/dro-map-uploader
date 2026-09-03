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

		private static readonly HashSet<string> Dro2Exported = new()
		{
			nameof(Transform),
			nameof(MeshFilter),
			nameof(MeshRenderer),
			nameof(MeshCollider),
			nameof(LODGroup),
			nameof(Light),
			"HDAdditionalLightData",
			nameof(GameMarkerData),
			nameof(Minimap),
			nameof(CacheData),
			"SceneObjectIDMapSceneAsset",
		};

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
			CheckFormatCoverage(report, format, rules);
			CheckMarkers(report, transforms, format);
			CheckLighting(report, transforms, format);
			CheckGeometry(report, transforms, format);
			CheckPhysics(report, transforms);
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
			foreach (var transform in transforms)
			{
				var components = transform.GetComponents<Component>();

				for (var i = 0; i < components.Length; i++)
				{
					var component = components[i];

					if (component == null)
					{
						report.AddCapped(MapValidationSeverity.Error, CategoryComponents, "missing-script",
							$"'{transform.name}' has a missing script in component slot {i}. Remove the slot or restore the script.",
							transform.gameObject);
						continue;
					}

					if (ModMapTestTool.ValidType(component, rules.data))
					{
						continue;
					}

					var typeName = component.GetType().Name;

					if (skipTypes != null && skipTypes.Contains(typeName))
					{
						continue;
					}

					report.AddCapped(MapValidationSeverity.Error, CategoryComponents, "unsupported-" + typeName,
						$"'{typeName}' on '{transform.name}' is not a supported component and will not be exported. " +
						"Remove it, or add it to Assets/Resources/MapSkipComponent if it is an editor only helper.",
						component);
				}
			}
		}

		private static void CheckBudget(MapValidationReport report, ValidItemData rules)
		{
			if (rules.data == null)
			{
				return;
			}

			for (var index = 0; index < rules.data.Count; index++)
			{
				var item = rules.data[index];
				item.ValidProcess();
				rules.data[index] = item;

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

				if (item.validComponentProcess is { isSuccess: false })
				{
					foreach (var line in SplitLines(item.validComponentProcess.processMessage))
					{
						report.Error(CategoryBudget, line);
					}
				}
			}
		}

		private static void CheckFormatCoverage(MapValidationReport report, FormatBuild format, ValidItemData rules)
		{
			if (format != FormatBuild.dro2 || rules.data == null)
			{
				return;
			}

			foreach (var item in rules.data)
			{
				if (item.current <= 0 || Dro2Exported.Contains(item.type))
				{
					continue;
				}

				report.Warning(CategoryFormat,
					$"'{item.type}' x{item.current} is supported by dro1 but is not part of the dro2 catalog - " +
					"it will not appear in the published mod. Build as dro1 if the map needs it.",
					item.components != null && item.components.Count > 0 ? item.components[0] : null);
			}
		}

		private static void CheckMarkers(MapValidationReport report, List<Transform> transforms, FormatBuild format)
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

				if (head == "spawnpoint")
				{
					spawnPoints.Add(marker);
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
					if (format == FormatBuild.dro1)
					{
						report.Error(CategoryMarkers,
							$"dro1 supports exactly one SpawnPoint and the map has {spawnPoints.Count}. " +
							"Remove the extras or build as dro2, which supports several.",
							spawnPoints[1]);
					}
					else
					{
						CheckSpawnPointNames(report, spawnPoints);
					}

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
						$"{group.Count()} spawn points are all named '{group.Key}'. dro2 exports the object name as " +
						"the spawn point's identity, so give each one a distinct, meaningful name.",
						group.First());
				}
			}
		}

		private static void CheckLighting(MapValidationReport report, List<Transform> transforms, FormatBuild format)
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

				if (format != FormatBuild.dro2 || !light.enabled)
				{
					continue;
				}

				if (light.type != LightType.Point && light.type != LightType.Spot)
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryLighting, "dro2-light-type",
						$"'{transform.name}' is a {light.type} light. dro2 exports Point and Spot lights only, so this " +
						"one will not reach the mod.",
						light);
				}
			}

			if (directional.Count > 1)
			{
				report.Warning(CategoryLighting,
					$"The map has {directional.Count} Directional Lights. Use one - several of them light the scene " +
					"differently in game than they do in the editor.",
					directional[1]);
			}
		}

		private static void CheckGeometry(MapValidationReport report, List<Transform> transforms, FormatBuild format)
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

				CheckLodGroup(report, transform, format);
			}
		}

		private static void CheckLodGroup(MapValidationReport report, Transform transform, FormatBuild format)
		{
			var lodGroup = transform.GetComponent<LODGroup>();

			if (lodGroup == null)
			{
				return;
			}

			if (lodGroup.lodCount > 8)
			{
				report.AddCapped(format == FormatBuild.dro2 ? MapValidationSeverity.Error : MapValidationSeverity.Warning,
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

		private static IEnumerable<string> SplitLines(string value)
		{
			return string.IsNullOrWhiteSpace(value)
				? Enumerable.Empty<string>()
				: value.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0);
		}
	}
}
