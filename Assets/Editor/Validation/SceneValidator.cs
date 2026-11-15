using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CarX.Modding.Creator.Editor;
using CarX.Modding.Creator.Runtime;
using UnityEditor;
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
		private static readonly string MaterialMarkerHead = MarkerData.MaterialHead.ToLowerInvariant();

		/// <summary>Render core variants with foliage lighting (MAT_FOLIAGE_ALPHATEST, MAT_FOLIAGE_SIMPLE_ALPHATEST).</summary>
		private const string FoliageVariantPrefix = "MAT_FOLIAGE_";

		/// <summary>Water repeats per meter of the surface (UV per meter x Tiling) that look right; the stock look is <see cref="ModWater.RecommendedRepeatsPerMeter"/>.</summary>
		private const float MinWaterRepeatsPerMeter = 0.01f;
		private const float MaxWaterRepeatsPerMeter = 0.25f;

		/// <summary>World area, m2, below which a water submesh has no surface to measure its UVs on.</summary>
		private const float MinWaterArea = 1e-4f;

		/// <summary>How many object names a message lists before "and N more".</summary>
		private const int ListedObjects = 3;

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
			CheckSounds(report, transforms);
			CheckWater(report, transforms);
			CheckMaterialMarkerCoverage(report, transforms);
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
            var animationRoots = CarX.Modding.Creator.Editor.VertexAnimationBaker.GetAnimationRoots(
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
						"This marker will be skipped during export. Supported types are SpawnPoint, Road, Animation and Material.", marker);
					continue;
				}

				if (head == "spawnpoint")
				{
					spawnPoints.Add(marker);
				}

				if (head == "animation")
				{
					var settings = marker.markerData.value as CarX.Modding.Creator.Runtime.AnimationMarkerSettings;
					var animator = settings?.animator != null ? settings.animator : marker.GetComponent<Animator>();
					if (animator == null || animator.runtimeAnimatorController == null || animator.runtimeAnimatorController.animationClips.Length == 0)
						report.Error(CategoryMarkers, "Animation marker requires an Animator with animation clips.", marker);
				}

				if (head == MaterialMarkerHead)
				{
					CheckMaterialMarker(report, marker);
				}

				if (head == "road" && UnavailableRoadSurfaces.TryGetValue(marker.markerData.param ?? string.Empty, out var replacement))
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "road-unavailable-surface",
						$"'{transform.name}' uses the surface '{marker.markerData.param}', which the game doesn't have, so its collider " +
						$"gets no surface in the game. Use '{replacement}' instead.",
						marker);
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

		/// <summary>
		/// Sound markers as the exporter takes them (active, enabled): a known sound of the kind, an .ogg or .wav clip,
		/// min distance not above max, one ambience and the limits of the sounds document.
		/// </summary>
		private static void CheckSounds(MapValidationReport report, List<Transform> transforms)
		{
			int emitters = 0;
			GameMarkerData firstAmbience = null;
			var files = new HashSet<string>(StringComparer.Ordinal);
			long filesBytes = 0;

			foreach (Transform transform in transforms)
			{
				if (!transform.gameObject.activeInHierarchy)
				{
					continue;
				}

				foreach (GameMarkerData marker in transform.GetComponents<GameMarkerData>())
				{
					if (!marker.enabled || marker.MarkerHead != MarkerData.SoundHead)
					{
						continue;
					}

					emitters++;

					if (marker.MarkerData is not SoundMarkerSettings settings)
					{
						report.AddCapped(MapValidationSeverity.Error, CategoryMarkers, "sound-no-settings",
							$"'{transform.name}' is a Sound marker without settings. Select the object to create them, then set the sound.", marker);
						continue;
					}

					SoundEmitterKind kind = settings.Kind(marker.MarkerParam);

					if (kind == SoundEmitterKind.Custom)
					{
						CheckSoundClip(report, marker, settings, files, ref filesBytes);
					}
					else
					{
						CheckSoundPreset(report, marker, settings, kind == SoundEmitterKind.Ambience ? ModSoundPresets.Ambience : ModSoundPresets.Point);
					}

					if (kind == SoundEmitterKind.Ambience)
					{
						if (firstAmbience == null)
						{
							firstAmbience = marker;
						}
						else
						{
							report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "sound-ambience-twice",
								$"'{transform.name}' is a second Sound/Ambience marker. A map plays one ambience: the first one, " +
								$"'{firstAmbience.name}'. Remove the other ambience markers.", marker);
						}
					}
					else if (settings.minDistance > settings.maxDistance)
					{
						report.AddCapped(MapValidationSeverity.Error, CategoryMarkers, "sound-distance",
							$"'{transform.name}': Min Distance {settings.minDistance} m is greater than Max Distance {settings.maxDistance} m. " +
							"Make Min Distance smaller.", marker);
					}
				}
			}

			if (emitters > SoundsMeta.MaxEmitters)
			{
				report.Error(CategoryMarkers,
					$"The map has {emitters} Sound markers, at most {SoundsMeta.MaxEmitters} are exported. Remove the sounds the map doesn't need.");
			}

			if (files.Count > SoundsMeta.MaxFiles)
			{
				report.Error(CategoryMarkers,
					$"Sound/Custom markers use {files.Count} audio files, at most {SoundsMeta.MaxFiles} are exported. Use fewer different clips.");
			}

			if (filesBytes > SoundsMeta.MaxFilesBytes)
			{
				report.Error(CategoryMarkers,
					$"The audio files of Sound/Custom markers take {filesBytes / (1024f * 1024f):0.#} MB, at most {SoundsMeta.MaxFilesBytes / (1024 * 1024)} MB " +
					"are exported. Shorten the clips or save them as .ogg.");
			}
		}

		private static void CheckSoundPreset(MapValidationReport report, GameMarkerData marker, SoundMarkerSettings settings,
			IReadOnlyList<ModSoundPresets.Preset> presets)
		{
			string preset = settings.preset?.Trim() ?? string.Empty;

			if (preset.Length == 0)
			{
				report.AddCapped(MapValidationSeverity.Error, CategoryMarkers, "sound-no-preset",
					$"'{marker.name}' has no sound selected. Select a sound in its {marker.MarkerParam} marker.", marker);
				return;
			}

			if (presets.All(item => item.id != preset))
			{
				report.AddCapped(MapValidationSeverity.Error, CategoryMarkers, "sound-unknown-preset",
					$"'{marker.name}' uses the sound '{preset}', which {marker.MarkerParam} doesn't have. Select a sound from the list.", marker);
			}
		}

		private static void CheckSoundClip(MapValidationReport report, GameMarkerData marker, SoundMarkerSettings settings, HashSet<string> files,
			ref long filesBytes)
		{
			string path = settings.clip != null ? AssetDatabase.GetAssetPath(settings.clip) : string.Empty;

			if (string.IsNullOrEmpty(path))
			{
				report.AddCapped(MapValidationSeverity.Error, CategoryMarkers, "sound-no-clip",
					$"'{marker.name}' is a Sound/Custom marker without an audio clip. Assign an .ogg or .wav clip.", marker);
				return;
			}

			if (!SoundsCollector.IsSupportedFile(path))
			{
				report.AddCapped(MapValidationSeverity.Error, CategoryMarkers, "sound-clip-format",
					$"'{marker.name}' plays '{Path.GetFileName(path)}'. Only .ogg and .wav files are exported: convert the clip to one of them.", marker);
				return;
			}

			if (files.Add(path))
			{
				filesBytes += new FileInfo(FileUtil.GetPhysicalPath(path)).Length;
			}
		}

		/// <summary>
		/// Water: one Water/Sea marker per map, counted as the exporter takes them (active, enabled). Binary only, for every renderer a WATER
		/// entry of a Material marker applies to (<see cref="MaterialMarkerCoverage"/>): no solid collider and a sensible UV density.
		/// </summary>
		private static void CheckWater(MapValidationReport report, List<Transform> transforms)
		{
			var seas = new List<GameMarkerData>();

			foreach (Transform transform in transforms)
			{
				if (!transform.gameObject.activeInHierarchy)
				{
					continue;
				}

				foreach (GameMarkerData marker in transform.GetComponents<GameMarkerData>())
				{
					if (marker.enabled && marker.MarkerHead == MarkerData.WaterHead && marker.MarkerParam == WaterMarkerSettings.SeaParam)
					{
						seas.Add(marker);
					}
				}
			}

			if (seas.Count > 1)
			{
				report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "water-sea-twice",
					$"The map has {seas.Count} {WaterMarkerSettings.SeaParam} markers: {DescribeObjects(seas.Select(sea => sea.name).ToList())}. " +
					"A map has one sea, so only the first one is used. Remove the other sea markers.", seas[1]);
			}

			if (report.format != FormatBuild.Binary)
			{
				return;
			}

			List<Transform> animationRoots = VertexAnimationBaker.GetAnimationRoots(transforms.Select(transform => transform.root).Distinct(), IsExcluded);
			var vertices = new List<Vector3>();
			var uvs = new List<Vector2>();
			var triangles = new List<int>();

			foreach ((MeshRenderer renderer, GameObject _, RenderCoreMaterialMarkerSettings settings) in MaterialMarkerCoverage.Collect(transforms, animationRoots))
			{
				if (settings == null)
				{
					continue;
				}

				Material[] materials = renderer.sharedMaterials;
				Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
				bool meshRead = false;
				bool colliderChecked = false;

				for (int subMesh = 0; subMesh < materials.Length && subMesh < mesh.subMeshCount; subMesh++)
				{
					RenderCoreMaterialOverride materialOverride = materials[subMesh] != null ? settings.Find(materials[subMesh]) : null;

					if (materialOverride == null || !ModWater.IsWater(materialOverride.variant))
					{
						continue;
					}

					if (!colliderChecked)
					{
						CheckWaterCollider(report, renderer, materials[subMesh]);
						colliderChecked = true;
					}

					// Edit mode reads any mesh; in Play mode only a readable one
					if (mesh.GetTopology(subMesh) != MeshTopology.Triangles || (!mesh.isReadable && Application.isPlaying))
					{
						continue;
					}

					if (!meshRead)
					{
						mesh.GetVertices(vertices);
						mesh.GetUVs(0, uvs);
						meshRead = true;
					}

					mesh.GetTriangles(triangles, subMesh);
					CheckWaterUvDensity(report, renderer, materials[subMesh], materialOverride, vertices, uvs, triangles);
				}
			}
		}

		/// <summary>Cars collide with a solid collider of the water mesh as with a road.</summary>
		private static void CheckWaterCollider(MapValidationReport report, MeshRenderer renderer, Material material)
		{
			foreach (Collider collider in renderer.GetComponents<Collider>())
			{
				if (!collider.enabled || collider.isTrigger)
				{
					continue;
				}

				report.AddCapped(MapValidationSeverity.Warning, CategoryPhysics, "water-collider",
					$"'{renderer.name}' has the water material '{material.name}' and a {collider.GetType().Name}: cars will drive on the water surface. " +
					"Remove the collider or tick Is Trigger.", collider);
				return;
			}
		}

		/// <summary>
		/// UV units per meter of the submesh (square root of its UV area over its world area) times Tiling: how many times per meter the water
		/// repeats. The suggested Tiling gives <see cref="ModWater.RecommendedRepeatsPerMeter"/>.
		/// </summary>
		private static void CheckWaterUvDensity(MapValidationReport report, MeshRenderer renderer, Material material,
			RenderCoreMaterialOverride materialOverride, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
		{
			Matrix4x4 localToWorld = renderer.transform.localToWorldMatrix;
			bool hasUvs = uvs.Count == vertices.Count;
			double worldArea = 0;
			double uvArea = 0;

			for (int i = 0; i + 2 < triangles.Count; i += 3)
			{
				int a = triangles[i];
				int b = triangles[i + 1];
				int c = triangles[i + 2];
				Vector3 origin = localToWorld.MultiplyPoint3x4(vertices[a]);
				Vector3 edgeB = localToWorld.MultiplyPoint3x4(vertices[b]) - origin;
				Vector3 edgeC = localToWorld.MultiplyPoint3x4(vertices[c]) - origin;
				worldArea += Vector3.Cross(edgeB, edgeC).magnitude * 0.5;

				if (hasUvs)
				{
					Vector2 uvB = uvs[b] - uvs[a];
					Vector2 uvC = uvs[c] - uvs[a];
					uvArea += Mathf.Abs(uvB.x * uvC.y - uvB.y * uvC.x) * 0.5;
				}
			}

			if (worldArea < MinWaterArea)
			{
				return;
			}

			if (uvArea <= 0)
			{
				report.AddCapped(MapValidationSeverity.Warning, CategoryGeometry, "water-uv",
					$"'{renderer.name}': the mesh has no UVs under the water material '{material.name}', so the waves don't show. " +
					"Add UVs in meters (1 UV unit = 1 m).", renderer);
				return;
			}

			float tiling = GetWaterValue(materialOverride, ModWater.TilingProperty, ModWater.DefaultTiling);
			float uvPerMeter = (float)Math.Sqrt(uvArea / worldArea);
			float repeats = uvPerMeter * tiling;

			if (repeats >= MinWaterRepeatsPerMeter && repeats <= MaxWaterRepeatsPerMeter)
			{
				return;
			}

			report.AddCapped(MapValidationSeverity.Warning, CategoryGeometry, "water-uv",
				$"'{renderer.name}': the water '{material.name}' repeats {repeats:0.####} times per meter ({uvPerMeter:0.###} UV units per meter x " +
				$"Tiling {tiling:0.####}); it looks right from {MinWaterRepeatsPerMeter} to {MaxWaterRepeatsPerMeter}. Set Tiling to about " +
				$"{ModWater.RecommendedRepeatsPerMeter / uvPerMeter:0.####} or use UVs in meters.", renderer);
		}

		/// <summary>Tiling and speeds of a WATER entry within the limits of the game water.</summary>
		private static void CheckWaterValues(MapValidationReport report, GameMarkerData marker, RenderCoreMaterialOverride materialOverride)
		{
			string name = marker.transform.name;
			string source = materialOverride.source.name;
			float tiling = GetWaterValue(materialOverride, ModWater.TilingProperty, ModWater.DefaultTiling);

			if (tiling < ModWater.MinTiling || tiling > ModWater.MaxTiling)
			{
				report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "water-tiling",
					$"'{name}': water '{source}' has Tiling {tiling}, outside {ModWater.MinTiling} to {ModWater.MaxTiling}. " +
					$"With UVs in meters use {ModWater.DefaultTiling}.", marker);
			}

			foreach (ModRenderCoreVariantProperty property in ModWater.Definition.properties)
			{
				if (property.name == ModWater.TilingProperty)
				{
					continue;
				}

				float speed = GetWaterValue(materialOverride, property.name, property.defaultValue.x);

				if (Mathf.Abs(speed) > ModWater.MaxSpeed)
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "water-speed",
						$"'{name}': water '{source}' has {property.label} {speed}, faster than {ModWater.MaxSpeed} UV units per second. " +
						$"Use a value from -{ModWater.MaxSpeed} to {ModWater.MaxSpeed}.", marker);
				}
			}
		}

		/// <summary>A value the entry stores, or the default the export writes for a missing one.</summary>
		private static float GetWaterValue(RenderCoreMaterialOverride materialOverride, string property, float defaultValue)
		{
			RenderCoreMaterialValue value = materialOverride.Find(property);
			return value != null ? value.vector.x : defaultValue;
		}

		/// <summary>
		/// Маркер Material: только Binary, есть что переопределять, вариант известен и доступен в модах, материал из рендерера, нет лишних
		/// значений, значения запечены для текущей ревизии варианта, задан diffuse, цвет вершин подходит варианту.
		/// </summary>
		private static void CheckMaterialMarker(MapValidationReport report, GameMarkerData marker)
		{
			var name = marker.transform.name;

			if (report.format != FormatBuild.Binary)
			{
				report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "material-marker-wavefront",
					$"'{name}' has a Material marker, but render core materials are exported only in the Binary format. " +
					"Wavefront ignores the marker and exports the original materials.", marker);
			}

			var settings = marker.markerData.value as RenderCoreMaterialMarkerSettings;

			if (settings == null || settings.overrides.Count == 0)
			{
				report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "material-marker-empty",
					$"'{name}' has a Material marker without materials. Press 'Add renderer materials' or remove the marker.", marker);
				return;
			}

			var rendererMaterials = RenderCoreMaterialMarkerSettingsDrawer.GetRendererMaterials(marker.gameObject);

			if (rendererMaterials.Count == 0)
			{
				report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "material-marker-no-renderer",
					$"'{name}' has a Material marker, but neither a MeshRenderer nor a LODGroup on the same object, so it changes nothing.", marker);
				return;
			}

			if (VertexAnimationBaker.GetAnimationRoots(marker.transform.root).Any(root => marker.transform.IsChildOf(root)))
			{
				report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "material-marker-animation",
					$"'{name}' is part of a baked vertex animation. Its Material marker is ignored: animated objects keep their own materials.", marker);
			}

			foreach (var materialOverride in settings.overrides)
			{
				if (materialOverride.source == null)
				{
					report.AddCapped(MapValidationSeverity.Error, CategoryMarkers, "material-marker-no-source",
						$"'{name}' has a Material marker entry without a source material. Assign it or remove the entry.", marker);
					continue;
				}

				var source = materialOverride.source.name;

				if (!ModRenderCoreVariants.TryFind(materialOverride.variant, out ModRenderCoreVariant variant))
				{
					report.AddCapped(MapValidationSeverity.Error, CategoryMarkers, "material-marker-variant",
						$"'{name}': material '{source}' has no supported render core variant ('{materialOverride.variant}').", marker);
					continue;
				}

				// Вариант есть в зеркале, но в модах не работает (например, MAT_INTERIOR без второго UV): экспорт его пропустит.
				if (!variant.Available)
				{
					report.AddCapped(MapValidationSeverity.Error, CategoryMarkers, "material-marker-variant-unavailable",
						$"'{name}': material '{source}' uses render core variant {variant.name}, which is unavailable in mods: " +
						$"{variant.unavailableReason}. Choose another variant.", marker);
					continue;
				}

				if (!rendererMaterials.Contains(materialOverride.source))
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "material-marker-unused",
						$"'{name}': material '{source}' is not used by the renderer, so its render core override is never applied.", marker);
				}

				CheckOrphanValues(report, marker, materialOverride, variant);
				CheckBakedRevision(report, marker, materialOverride, variant);

				if (ModWater.IsWater(variant.name))
				{
					CheckWaterValues(report, marker, materialOverride);
				}

				ModRenderCoreVariantProperty diffuse = FindDiffuse(variant);

				if (diffuse != null && materialOverride.Find(diffuse.name)?.texture == null)
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "material-marker-diffuse",
						$"'{name}': render core material '{source}' ({variant.name}) has no diffuse texture and will render white. " +
						"Press 'Export from material' or assign the texture.", marker);
				}
			}

			CheckMaterialMarkerVertexColors(report, marker, settings);
		}

		/// <summary>Сохранённые значения свойств, которых у варианта нет: остались от другого варианта или удалены из rendercore.</summary>
		private static void CheckOrphanValues(MapValidationReport report, GameMarkerData marker, RenderCoreMaterialOverride materialOverride,
			ModRenderCoreVariant variant)
		{
			if (materialOverride.values == null)
			{
				return;
			}

			List<string> orphans = materialOverride.values
				.Where(value => value != null && !variant.TryGetProperty(value.name, out _))
				.Select(value => string.IsNullOrEmpty(value.name) ? "<unnamed>" : value.name)
				.ToList();

			if (orphans.Count == 0)
			{
				return;
			}

			report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "material-marker-orphan-value",
				$"'{marker.transform.name}': render core material '{materialOverride.source.name}' ({variant.name}) stores values that the variant " +
				$"does not have: {string.Join(", ", orphans)}. They are not exported. Press 'Export from material' or 'Remove' next to " +
				"'Unused values' in the marker.", marker);
		}

		/// <summary>
		/// Binary only, a hint: a material that looks like foliage (double-sided, alpha-clipped, see <see cref="RenderCoreMaterialBaker.IsFoliage"/>)
		/// is mapped by an applied marker entry to a non-foliage alpha-test variant. The same flags fit fences, grates and cables, and an explicit
		/// entry is the author's decision, so this is information, not a warning.
		/// </summary>
		private static void CheckFoliageVariant(MapValidationReport report, GameObject markerObject, Material source, ModRenderCoreVariant variant)
		{
			report.AddCapped(MapValidationSeverity.Info, CategoryMarkers, "material-marker-foliage-variant",
				$"'{markerObject.name}': material '{source.name}' is double-sided and alpha-tested (vegetation?) and is mapped to {variant.name}. " +
				"If it is vegetation, MAT_FOLIAGE_ALPHATEST (with a normal map) or MAT_FOLIAGE_SIMPLE_ALPHATEST (without) gives it foliage lighting; " +
				$"for a fence, grate or cable keep {variant.name}.", markerObject);
		}

		private static bool IsFoliageVariant(ModRenderCoreVariant variant)
		{
			return variant.name.StartsWith(FoliageVariantPrefix, StringComparison.Ordinal);
		}

		/// <summary>
		/// Значения записи запечены для более старой ревизии варианта, чем в таблице SDK (0 — запись до появления поля — считается
		/// ревизией 1). Экспорт пишет в документ ревизию запекания, поэтому игра с текущей таблицей возьмёт запасной материал, пока автор
		/// не повторит Export from material. Публичный, чтобы тесты проверяли правило на варианте с ревизией выше 1.
		/// </summary>
		public static void CheckBakedRevision(MapValidationReport report, GameMarkerData marker, RenderCoreMaterialOverride materialOverride,
			ModRenderCoreVariant variant)
		{
			if (materialOverride.BakedRevision >= variant.revision)
			{
				return;
			}

			string source = materialOverride.source != null ? materialOverride.source.name : "<no material>";

			report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "material-marker-stale-bake",
				$"'{marker.transform.name}': render core material '{source}' ({variant.name}) was exported from material for revision " +
				$"{materialOverride.BakedRevision}, but this SDK has revision {variant.revision} of the variant. The game uses the fallback " +
				"material until you re-run Export from material on this entry.", marker);
		}

		/// <summary>Свойство diffuse варианта: первое с источником Diffuse на слое 0 (имя слота у вариантов может отличаться).</summary>
		private static ModRenderCoreVariantProperty FindDiffuse(ModRenderCoreVariant variant)
		{
			foreach (ModRenderCoreVariantProperty property in variant.properties)
			{
				if (property.source == ModRenderCoreSource.Diffuse && property.layer == 0)
				{
					return property;
				}
			}

			return null;
		}

		/// <summary>
		/// Цвет вершин мешей маркера. MAT_LAYERS4 (LayerBlend) смешивает слои по цвету вершин: меш без цвета экспортируется белым, и виден
		/// только четвёртый слой. Клиент делает белым весь меш с эмиссией, но не меш с LayerBlend: на общем меше эмиссия умножится на маску слоёв.
		/// Эмиссия — и от варианта переопределения, и от материала HDRP без применимого переопределения (его экспорт пишет эмиссию в .mtl).
		/// </summary>
		private static void CheckMaterialMarkerVertexColors(MapValidationReport report, GameMarkerData marker,
			RenderCoreMaterialMarkerSettings settings)
		{
			foreach (MeshRenderer renderer in GetMarkedRenderers(marker.gameObject))
			{
				MeshFilter filter = renderer.GetComponent<MeshFilter>();
				Mesh mesh = filter != null ? filter.sharedMesh : null;

				if (mesh == null)
				{
					continue;
				}

				string layerBlend = null;
				string emission = null;

				foreach (Material material in renderer.sharedMaterials)
				{
					if (material == null)
					{
						continue;
					}

					ModRenderCoreVertexColors usage = GetVertexColorUsage(settings, material, out string usedBy);

					if (usage == ModRenderCoreVertexColors.LayerBlend)
					{
						layerBlend ??= usedBy;
					}
					else if (usage == ModRenderCoreVertexColors.EmissionMultiplier)
					{
						emission ??= usedBy;
					}
				}

				if (layerBlend == null)
				{
					continue;
				}

				if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Color))
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "material-marker-vertex-colors",
						$"'{renderer.name}': mesh '{mesh.name}' has no vertex colors, but {layerBlend} blends its layers by them " +
						"(RGB - layer mask, A - albedo tint). It is exported white, so only the fourth layer shows. Paint vertex colors in the mesh.",
						marker);
				}

				if (emission != null)
				{
					report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "material-marker-vertex-colors",
						$"'{renderer.name}': mesh '{mesh.name}' mixes {layerBlend} and {emission} submeshes. The game keeps the layer blend " +
						"vertex colors for the whole mesh, so the emission is multiplied by them. Move the emissive part to a separate object " +
						"without layer blend materials: build optimization merges layer blend objects only with each other.",
						marker);
				}
			}
		}

		/// <summary>
		/// Как материал рендерера использует цвет вершин в игре и чем это вызвано (для сообщения). Применимое переопределение маркера —
		/// по своему варианту. Без него (нет записи, вариант неизвестен или недоступен) экспортируется исходный материал, и его эмиссию
		/// определяет то же правило, что и экспорт (<see cref="RenderCoreMaterialOverrides.GetVertexColorUsage"/>).
		/// </summary>
		private static ModRenderCoreVertexColors GetVertexColorUsage(RenderCoreMaterialMarkerSettings settings, Material material, out string usedBy)
		{
			RenderCoreMaterialOverride materialOverride = settings.Find(material);

			if (materialOverride != null && ModRenderCoreVariants.TryGet(materialOverride.variant, out ModRenderCoreVariant variant))
			{
				usedBy = variant.name;
				return variant.vertexColors;
			}

			usedBy = $"emissive material '{material.name}'";
			return RenderCoreMaterialOverrides.GetVertexColorUsage(material);
		}

		/// <summary>
		/// Рендереры, к которым применяется маркер: MeshRenderer объекта и LOD его LODGroup. Рендерер LOD со своим маркером Material
		/// берёт свой маркер, как и в экспорте (RenderCoreMaterialOverrides.Resolve).
		/// </summary>
		private static IEnumerable<MeshRenderer> GetMarkedRenderers(GameObject gameObject)
		{
			var renderers = new List<MeshRenderer>();
			MeshRenderer ownRenderer = gameObject.GetComponent<MeshRenderer>();

			if (ownRenderer != null)
			{
				renderers.Add(ownRenderer);
			}

			LODGroup lodGroup = gameObject.GetComponent<LODGroup>();

			if (lodGroup == null)
			{
				return renderers;
			}

			foreach (LOD lod in lodGroup.GetLODs())
			{
				if (lod.renderers == null)
				{
					continue;
				}

				foreach (Renderer renderer in lod.renderers)
				{
					if (renderer is MeshRenderer meshRenderer && meshRenderer != null && !renderers.Contains(meshRenderer) &&
						(meshRenderer.gameObject == gameObject || RenderCoreMaterialOverrides.FindSettings(meshRenderer.gameObject) == null))
					{
						renderers.Add(meshRenderer);
					}
				}
			}

			return renderers;
		}

		/// <summary>
		/// Binary only: Material marker coverage of the exported renderers, by the exporter's rules (<see cref="MaterialMarkerCoverage"/>).
		/// A source material mapped to different variants by different marker entries renders differently from object to object (for example
		/// on one LOD).
		/// Only applied entries count: an entry with an unknown or unavailable variant is reported by <see cref="CheckMaterialMarker"/>.
		/// </summary>
		private static void CheckMaterialMarkerCoverage(MapValidationReport report, List<Transform> transforms)
		{
			if (report.format != FormatBuild.Binary)
			{
				return;
			}

			List<Transform> animationRoots = VertexAnimationBaker.GetAnimationRoots(transforms.Select(transform => transform.root).Distinct(), IsExcluded);
			var foliage = new Dictionary<Material, bool>();
			var variantUses = new Dictionary<Material, Dictionary<string, List<GameObject>>>();
			var foliageHints = new HashSet<(GameObject, Material)>();

			foreach ((MeshRenderer renderer, GameObject markerObject, RenderCoreMaterialMarkerSettings settings) in
				MaterialMarkerCoverage.Collect(transforms, animationRoots))
			{
				foreach (Material material in renderer.sharedMaterials)
				{
					if (material == null)
					{
						continue;
					}

					RenderCoreMaterialOverride materialOverride = settings != null ? settings.Find(material) : null;

					if (materialOverride == null)
					{
						continue;
					}

					if (ModRenderCoreVariants.TryGet(materialOverride.variant, out ModRenderCoreVariant variant))
					{
						if (!variantUses.TryGetValue(material, out Dictionary<string, List<GameObject>> uses))
						{
							uses = new Dictionary<string, List<GameObject>>();
							variantUses.Add(material, uses);
						}

						AddDistinct(uses, variant.name, markerObject);

						if (variant.renderType == ModRenderCoreRenderType.AlphaTest && !IsFoliageVariant(variant) && IsFoliage(material, foliage) &&
							foliageHints.Add((markerObject, material)))
						{
							CheckFoliageVariant(report, markerObject, material, variant);
						}
					}
				}
			}

			ReportVariantMismatches(report, variantUses);
		}

		/// <summary>
		/// One warning per source material that applied marker entries map to different variants. The context is a marker of the least used
		/// variant: usually the odd one out.
		/// </summary>
		private static void ReportVariantMismatches(MapValidationReport report, Dictionary<Material, Dictionary<string, List<GameObject>>> variantUses)
		{
			foreach (KeyValuePair<Material, Dictionary<string, List<GameObject>>> pair in variantUses)
			{
				if (pair.Value.Count < 2)
				{
					continue;
				}

				string uses = string.Join("; ", pair.Value.Select(use => $"{use.Key} on {DescribeObjects(use.Value.Select(marker => marker.name).ToList())}"));
				GameObject context = pair.Value.OrderBy(use => use.Value.Count).First().Value[0];

				report.AddCapped(MapValidationSeverity.Warning, CategoryMarkers, "material-marker-variant-mismatch",
					$"Material '{pair.Key.name}' is mapped to different render core variants by Material markers: {uses}. Objects with this material " +
					"render differently (for example, one LOD loses foliage lighting). Use one variant for this material in all marker entries.", context);
			}
		}

		private static bool IsFoliage(Material material, Dictionary<Material, bool> cache)
		{
			if (!cache.TryGetValue(material, out bool foliage))
			{
				foliage = RenderCoreMaterialBaker.IsFoliage(material);
				cache.Add(material, foliage);
			}

			return foliage;
		}

		private static void AddDistinct<TKey, TValue>(Dictionary<TKey, List<TValue>> lists, TKey key, TValue value)
		{
			if (!lists.TryGetValue(key, out List<TValue> list))
			{
				list = new List<TValue>();
				lists.Add(key, list);
			}

			if (!list.Contains(value))
			{
				list.Add(value);
			}
		}

		/// <summary>"'A'", "'A', 'B'" or "'A', 'B', 'C' and 4 more objects".</summary>
		private static string DescribeObjects(List<string> names)
		{
			string listed = string.Join(", ", names.Take(ListedObjects).Select(name => $"'{name}'"));
			int more = names.Count - ListedObjects;

			return more > 0 ? $"{listed} and {more} more object{(more == 1 ? string.Empty : "s")}" : listed;
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

		// Road surfaces the tool still offers for old scenes, but the game has no template for, with the closest one it has.
		private static readonly Dictionary<string, string> UnavailableRoadSurfaces = new()
		{
			{ "Road/default", "Road/asphalt" },
			{ "Road/ice_snowy", "Road/road_icy" },
			{ "Road/pavement", "Road/paving_slab" },
		};

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

				if (light.type == LightType.Directional)
				{
					// A preview sun is expected: the game lights the map with its own sun and sky.
					report.AddCapped(MapValidationSeverity.Info, CategoryLighting, "Wavefront/Binary-light-type",
						$"'{transform.name}' is a Directional light. It lights the SDK preview only; the game uses its own sun, " +
						"so it is not exported.",
						light);
				}
				else if (light.type != LightType.Point && light.type != LightType.Spot)
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
            var config = MapManagerConfig.instance.mapMetaConfigValue;
            var settings = (config?.optimization ?? new CarX.Modding.Creator.Runtime.BuildOptimizationSettings()).Snapshot();
            if (!settings.enabled)
                report.Warning(CategoryGeometry,
                    "Build optimization is off: static meshes and colliders are exported as-is, without sector chunking, collider simplification or LODs. " +
                    "Large maps get more draw calls, heavier physics and longer loading. Enable it in Build > Optimization.", config);
            foreach (var t in transforms)
            {
                if (!CarX.Modding.Creator.Editor.SceneExportOptimization.IsEligible(t)) continue;
                // Meshes that optimization splits into sectors fit the budget after export; warn only about the ones
                // that are exported as they are.
                void Check(Mesh mesh, Component context, string kind, bool partitioned)
                {
                    if (mesh == null || (settings.enabled && partitioned)) return;
                    long triangles = 0;
                    for (int i = 0; i < mesh.subMeshCount; i++) if (mesh.GetTopology(i) == MeshTopology.Triangles) triangles += mesh.GetIndexCount(i) / 3;
                    var bounds = CarX.Modding.Creator.Editor.SceneExportOptimization.WorldBounds(mesh, t.localToWorldMatrix);
                    var size = bounds.size;
                    if (triangles <= settings.maxTriangles && Mathf.Max(size.x, Mathf.Max(size.y, size.z)) <= settings.sectorSize) return;
                    report.AddCapped(MapValidationSeverity.Warning, CategoryGeometry, "sector-" + kind,
                        $"'{t.name}' ({mesh.name}): static {kind}, {triangles:N0} triangles, world bounds {size.x:F1} x {size.y:F1} x {size.z:F1} m. " +
                        $"Exceeds sector budget ({settings.sectorSize:F0} m / {settings.maxTriangles:N0} triangles). Review Build > Optimization; size alone does not prove a performance issue.", context);
                }
                foreach (var collider in t.GetComponents<MeshCollider>())
                    if (collider.enabled && !collider.isTrigger && !collider.convex) Check(collider.sharedMesh, collider, "collider", settings.sectorColliders);
                var renderer = t.GetComponent<MeshRenderer>(); var filter = t.GetComponent<MeshFilter>();
                if (renderer != null && renderer.enabled && filter != null) Check(filter.sharedMesh, renderer, "render mesh", settings.sectorRenderMeshes);
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
						$"'{transform.name}': Bounds Size is {size}. Set it to the size of the map in world units, " +
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
