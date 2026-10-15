using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Editor.Validation;
using NUnit.Framework;
using Plugins.CarX.Modding.Creator.Runtime;
using UnityEngine;

namespace MapUploader.Tests
{
	public class SceneValidatorTests
	{
		private readonly List<GameObject> m_created = new();
		private readonly List<UnityEngine.Object> m_assets = new();

		[TearDown]
		public void DestroyCreatedObjects()
		{
			foreach (var gameObject in m_created)
			{
				if (gameObject != null)
				{
					UnityEngine.Object.DestroyImmediate(gameObject);
				}
			}

			foreach (UnityEngine.Object asset in m_assets)
			{
				if (asset != null)
				{
					UnityEngine.Object.DestroyImmediate(asset);
				}
			}

			m_created.Clear();
			m_assets.Clear();
		}

		[Test]
		public void ReportsAMissingSpawnPoint()
		{
			var root = NewObject("Track");

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "no SpawnPoint marker"));
		}

		[Test]
		public void AcceptsASingleSpawnPoint()
		{
			var root = NewObject("Track");
			NewMarker("Start", "SpawnPoint", root);

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "no SpawnPoint marker"));
		}

		[Test]
		public void AllowsSeveralSpawnPoints()
		{
			var root = NewObject("Track");
			NewMarker("StartA", "SpawnPoint", root);
			NewMarker("StartB", "SpawnPoint", root);

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsFalse(report.Issues.Any(issue => issue.category == SceneValidator.CategoryMarkers && issue.severity == MapValidationSeverity.Error));
			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "are all named"));
		}

		[Test]
		public void WarnsAboutSpawnPointsSharingAName()
		{
			var root = NewObject("Track");
			NewMarker("Start", "SpawnPoint", root);
			NewMarker("Start", "SpawnPoint", root);

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "are all named 'Start'"));
		}

		[Test]
		public void ReportsARoadMarkerWithNoCollider()
		{
			var root = NewObject("Track");
			NewMarker("Asphalt", "Road", root);

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "marked as Road but has no Collider"));
		}

		[Test]
		public void AcceptsARoadMarkerWithAColliderOnAChild()
		{
			var root = NewObject("Track");
			var road = NewMarker("Asphalt", "Road", root);
			NewObject("Surface", road.gameObject, typeof(BoxCollider));

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "marked as Road but has no Collider"));
		}

		[Test]
		public void ReportsAMarkerWithNoTypeSelected()
		{
			var root = NewObject("Track");
			NewMarker("Unset", string.Empty, root);

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "no type selected"));
		}

		[Test]
		public void AcceptsPrimitiveColliders()
		{
			var root = NewObject("Track");
			NewObject("Crate", root, typeof(BoxCollider));
			NewObject("Sphere", root, typeof(SphereCollider));
			NewObject("Capsule", root, typeof(CapsuleCollider));

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsFalse(report.Issues.Any(issue => issue.category == SceneValidator.CategoryFormat));
		}

		[Test]
		public void ReportsAnUnsupportedComponent()
		{
			var root = NewObject("Track");
			NewObject("Speaker", root, typeof(AudioSource));

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryComponents, "'AudioSource' on 'Speaker'"));
		}

		[Test]
		public void SkipsComponentsOnTheSkipList()
		{
			var root = NewObject("Track");
			NewObject("Speaker", root, typeof(AudioSource));

			var report = Validate(FormatBuild.Wavefront, new[] { root }, new List<string> { nameof(AudioSource) });

			Assert.IsFalse(Has(report, SceneValidator.CategoryComponents, "'AudioSource' on 'Speaker'"));
		}

		[Test]
		public void SkipsAnythingTaggedGarbage()
		{
			var root = NewObject("Track");
			var scratch = NewObject("Scratch", root, typeof(AudioSource));
			scratch.tag = "Garbage";

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryComponents, "'AudioSource' on 'Scratch'"));
		}

		[Test]
		public void ReportsMoreThanOneDirectionalLight()
		{
			var root = NewObject("Track");
			NewLight("SunA", LightType.Directional, root);
			NewLight("SunB", LightType.Directional, root);

			var report = Validate(FormatBuild.Binary, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryLighting, "2 Directional Lights"));
		}

		[Test]
		public void WarnsAboutUnsupportedLightTypes()
		{
			var root = NewObject("Track");
			NewLight("Sun", LightType.Directional, root);

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryLighting, "Wavefront/Binary exports Point and Spot lights only"));
		}

		[Test]
		public void AcceptsPointAndSpotLights()
		{
			var root = NewObject("Track");
			NewLight("Lamp", LightType.Point, root);
			NewLight("Spot", LightType.Spot, root);

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryLighting, "Wavefront/Binary exports Point and Spot lights only"));
		}

		[Test]
		public void ReportsAConcaveColliderDrivenByADynamicRigidbody()
		{
			var root = NewObject("Track");
			var prop = NewObject("Barrel", root, typeof(MeshCollider), typeof(Rigidbody));
			prop.GetComponent<MeshCollider>().convex = false;
			prop.GetComponent<Rigidbody>().isKinematic = false;

			var report = Validate(FormatBuild.Binary, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryPhysics, "non-convex MeshCollider driven by a non-kinematic Rigidbody"));
		}

		[Test]
		public void AcceptsAConcaveColliderOnAKinematicRigidbody()
		{
			var root = NewObject("Track");
			var prop = NewObject("Barrel", root, typeof(MeshCollider), typeof(Rigidbody));
			prop.GetComponent<Rigidbody>().isKinematic = true;

			var report = Validate(FormatBuild.Binary, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryPhysics, "non-convex MeshCollider driven by a non-kinematic Rigidbody"));
		}

		[Test]
		public void ReportsAMinimapThatWasNeverSetUp()
		{
			var root = NewObject("Track");
			NewObject("Minimap", root, typeof(Minimap));

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryMinimap, "Bound Size is"));
			Assert.IsTrue(Has(report, SceneValidator.CategoryMinimap, "no textures assigned"));
		}

		[Test]
		public void ReportsEveryProblemRatherThanTheFirst()
		{
			var root = NewObject("Track");
			NewObject("SpeakerA", root, typeof(AudioSource));
			NewObject("SpeakerB", root, typeof(AudioSource));
			NewMarker("Asphalt", "Road", root);

			var report = Validate(FormatBuild.Wavefront, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryComponents, "'AudioSource' on 'SpeakerA'"));
			Assert.IsTrue(Has(report, SceneValidator.CategoryComponents, "'AudioSource' on 'SpeakerB'"));
			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "marked as Road but has no Collider"));
			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "no SpawnPoint marker"));
		}

		[Test]
		public void RecordsPathsRelativeToTheGivenRoot()
		{
			var root = NewObject("Track");
			var branch = NewObject("Props", root);
			NewObject("Speaker", branch, typeof(AudioSource));

			var report = new MapValidationReport();
			SceneValidator.Validate(report, new[] { root }, FormatBuild.Wavefront, FreshRules(), new List<string>(), root.transform);

			var issue = report.Issues.First(i => i.message.Contains("'AudioSource' on 'Speaker'"));
			Assert.AreEqual("Props/Speaker", issue.objectPath);
		}

		[Test]
		public void CountsComponentsIntoTheBudget()
		{
			var root = NewObject("Track");
			NewObject("CrateA", root, typeof(BoxCollider));
			NewObject("CrateB", root, typeof(BoxCollider));

			var rules = FreshRules();
			SceneValidator.Validate(new MapValidationReport(), new[] { root }, FormatBuild.Binary, rules, new List<string>());

			var boxes = rules.data.First(item => item.type == nameof(BoxCollider));
			Assert.AreEqual(2, boxes.current);
		}

		private static ValidItemData FreshRules()
		{
			return MapSceneRules.ComponentRules.CloneWithLimits(4096f, 24f);
		}

		[TestCase("Ambient")]
		[TestCase("TimeObjectActivator")]
		[TestCase("NetworkObject")]
		public void WarnsAndSkipsMarkersWithoutClientSupport(string head)
		{
			var root = NewObject("Track");
			NewMarker("Old marker", head, root);
			var report = Validate(FormatBuild.Binary, root);
			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "unsupported marker '" + head + "'"));
			Assert.IsTrue(report.Issues.Where(issue => issue.message.Contains("unsupported marker '" + head + "'"))
				.All(issue => issue.severity == MapValidationSeverity.Warning));
			Assert.IsFalse(MarkerData.paramEditor.Any(item => MarkerData.GetHeadTarget(item) == head));
		}

		[TestCase(typeof(FixedJoint))]
		[TestCase(typeof(Canvas))]
		[TestCase(typeof(ParticleSystem))]
		public void SkipsComponentsWithoutAnExporter(Type type)
		{
			var root = NewObject("Track");
			NewObject("Unsupported", root, type);
			var report = Validate(FormatBuild.Binary, root);
            Assert.IsTrue(Has(report, SceneValidator.CategoryComponents, "'" + type.Name + "' on 'Unsupported'"));
            Assert.IsFalse(report.Issues.Any(issue => issue.category == SceneValidator.CategoryComponents && issue.severity != MapValidationSeverity.Info));
		}

		[Test]
		public void PrimitiveCollisionDoesNotRequireAMeshCollider()
		{
			var root = NewObject("Track");
			NewObject("Floor", root, typeof(BoxCollider));
			Assert.IsFalse(Has(Validate(FormatBuild.Binary, root), SceneValidator.CategoryBudget, "'MeshCollider'"));
		}

		[Test]
		public void UnmarkedAnimatorIsSkippedWithoutAComponentError()
		{
			var root = NewObject("Track");
			var flag = NewObject("Flag", root, typeof(Animator));
			var report = Validate(FormatBuild.Binary, root);
            Assert.IsTrue(Has(report, SceneValidator.CategoryComponents, "requires a GameMarkerData Animation"));
            Assert.IsFalse(report.Issues.Any(issue => issue.category == SceneValidator.CategoryComponents && issue.severity != MapValidationSeverity.Info));
			flag.AddComponent<GameMarkerData>().markerData = new MarkerData { head = "Animation", param = "Animation" };
			Assert.IsFalse(Has(Validate(FormatBuild.Binary, root), SceneValidator.CategoryComponents, "requires a GameMarkerData Animation"));
		}

		[Test]
		public void AnimationSelectionSurvivesRemovedMenuEntries()
		{
			var saved = JsonUtility.FromJson<MarkerData>("{\"head\":\"Animation\",\"param\":\"Animation\",\"index\":42}");
			var selected = MarkerData.FindEditorIndex(saved.head, saved.param);
			Assert.AreEqual("Animation", MarkerData.paramEditor[selected]);
			Assert.AreNotEqual(saved.index, selected);
			Assert.AreEqual(-1, MarkerData.FindEditorIndex("Ambient", "Ambient/Crowd"));
		}

		[TestCase(FormatBuild.Wavefront, 0, false)]
		[TestCase(FormatBuild.Wavefront, 1, false)]
		[TestCase(FormatBuild.Wavefront, 2, false)]
		[TestCase(FormatBuild.Binary, 0, false)]
		[TestCase(FormatBuild.Binary, 1, false)]
		[TestCase(FormatBuild.Binary, 2, false)]
		public void PreviewEnvironmentIsOptional(FormatBuild format, int count, bool expectsBudgetError)
		{
			var root = NewObject("Track");
			for (int i = 0; i < count; i++)
				NewObject("Preview" + i, root, typeof(UnityEngine.Rendering.Volume), typeof(ReflectionProbe));

			var report = Validate(format, root);
			foreach (var type in new[] { "Volume", nameof(ReflectionProbe) })
			{
				Assert.AreEqual(expectsBudgetError, Has(report, SceneValidator.CategoryBudget, "'" + type + "'"), type);
				Assert.IsFalse(report.Issues.Any(issue => issue.message.Contains("'" + type + "'") &&
					issue.severity != MapValidationSeverity.Info), type);
				Assert.AreEqual(count > 0, Has(report, SceneValidator.CategoryFormat, "'" + type + "'"), type);
			}
		}

		[TestCase(0)]
		[TestCase(1)]
		public void SwitchingFormatsKeepsPreviewComponentsOptional(int count)
		{
			var root = NewObject("Track");
			for (int i = 0; i < count; i++)
				NewObject("Preview" + i, root, typeof(UnityEngine.Rendering.Volume), typeof(ReflectionProbe));
			var rules = FreshRules();
			SceneValidator.Validate(new MapValidationReport(), new[] { root }, FormatBuild.Wavefront, rules, new List<string>());
			var report = new MapValidationReport();
			SceneValidator.Validate(report, new[] { root }, FormatBuild.Binary, rules, new List<string>());
			Assert.AreEqual(false, Has(report, SceneValidator.CategoryBudget, "'Volume'"));
			Assert.AreEqual(false, Has(report, SceneValidator.CategoryBudget, "'ReflectionProbe'"));
		}

		[Test]
		public void ReportsAnUnknownRenderCoreVariant()
		{
			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: false), NewOverride("MAT_NO_SUCH_VARIANT"));

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.IsTrue(HasIssue(report, MapValidationSeverity.Error, "has no supported render core variant ('MAT_NO_SUCH_VARIANT')"));
		}

		[Test]
		public void ReportsAnUnavailableRenderCoreVariantWithItsReason()
		{
			ModRenderCoreVariant unavailable = ModRenderCoreVariants.All.FirstOrDefault(variant => !variant.Available);

			if (unavailable == null)
			{
				Assert.Ignore("The render core mirror has no unavailable variant.");
			}

			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), NewOverride(unavailable.name));

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.IsTrue(HasIssue(report, MapValidationSeverity.Error, unavailable.name + ", which is unavailable in mods: " + unavailable.unavailableReason));
			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "has no supported render core variant"));
		}

		[Test]
		public void WarnsAboutStoredValuesTheVariantDoesNotHave()
		{
			ModRenderCoreVariant variant = FirstOffered(candidate => candidate.properties.Count > 0, "non-empty");
			RenderCoreMaterialOverride materialOverride = NewOverride(variant.name);
			materialOverride.GetOrAdd(variant.properties[0]);
			materialOverride.values.Add(new RenderCoreMaterialValue { name = "u_sRemovedProperty" });
			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), materialOverride);

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			// В списке только лишнее значение: свойство варианта в нём не упоминается.
			Assert.IsTrue(HasIssue(report, MapValidationSeverity.Warning, "does not have: u_sRemovedProperty. They are not exported."));
		}

		[Test]
		public void AcceptsStoredValuesOfTheVariantOnly()
		{
			ModRenderCoreVariant variant = FirstOffered(candidate => candidate.properties.Count > 0, "non-empty");
			RenderCoreMaterialOverride materialOverride = NewOverride(variant.name);

			foreach (ModRenderCoreVariantProperty property in variant.properties)
			{
				materialOverride.GetOrAdd(property);
			}

			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), materialOverride);

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "does not have:"));
		}

		[Test]
		public void FindsTheDiffuseTextureByItsRoleAndLayer()
		{
			ModRenderCoreVariant variant = FirstOffered(candidate => candidate.properties.Count(IsDiffuse) > 1, "layered diffuse");
			RenderCoreMaterialOverride materialOverride = NewOverride(variant.name);

			// Текстуры всех слоёв, кроме нулевого: diffuse по-прежнему не задан.
			foreach (ModRenderCoreVariantProperty property in variant.properties.Where(property => IsDiffuse(property) && property.layer != 0))
			{
				materialOverride.GetOrAdd(property).texture = NewTexture();
			}

			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), materialOverride);
			Assert.IsTrue(HasIssue(Validate(FormatBuild.Binary, root), MapValidationSeverity.Warning, "has no diffuse texture"));

			ModRenderCoreVariantProperty baseLayer = variant.properties.First(property => IsDiffuse(property) && property.layer == 0);
			materialOverride.GetOrAdd(baseLayer).texture = NewTexture();
			Assert.IsFalse(Has(Validate(FormatBuild.Binary, root), SceneValidator.CategoryMarkers, "has no diffuse texture"));
		}

		[TestCase(false, true)]
		[TestCase(true, false)]
		public void WarnsAboutALayerBlendVariantOnAMeshWithoutVertexColors(bool withColors, bool expectsWarning)
		{
			ModRenderCoreVariant layerBlend = FirstOffered(candidate => candidate.vertexColors == ModRenderCoreVertexColors.LayerBlend, "layer blend");
			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors), NewOverride(layerBlend.name));

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.AreEqual(expectsWarning, HasIssue(report, MapValidationSeverity.Warning, "has no vertex colors, but " + layerBlend.name));
		}

		[Test]
		public void WarnsAboutLayerBlendAndEmissionSubmeshesOnOneMesh()
		{
			ModRenderCoreVariant layerBlend = FirstOffered(candidate => candidate.vertexColors == ModRenderCoreVertexColors.LayerBlend, "layer blend");
			ModRenderCoreVariant emission = FirstOffered(candidate => candidate.vertexColors == ModRenderCoreVertexColors.EmissionMultiplier, "emission");
			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 2, withColors: true), NewOverride(layerBlend.name), NewOverride(emission.name));

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.IsTrue(HasIssue(report, MapValidationSeverity.Warning, $"mixes {layerBlend.name} and {emission.name} submeshes"));
			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "has no vertex colors"));
		}

		// Отдельные объекты остаются отдельными мешами и после оптимизации сборки: она склеивает объекты с LayerBlend только между собой.
		[Test]
		public void AcceptsLayerBlendAndEmissionOnSeparateMeshes()
		{
			ModRenderCoreVariant layerBlend = FirstOffered(candidate => candidate.vertexColors == ModRenderCoreVertexColors.LayerBlend, "layer blend");
			ModRenderCoreVariant emission = FirstOffered(candidate => candidate.vertexColors == ModRenderCoreVertexColors.EmissionMultiplier, "emission");
			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), NewOverride(layerBlend.name));
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), NewOverride(emission.name));

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "submeshes"));
		}

		/// <summary>
		/// Материал без применимого переопределения (записи нет или её вариант неизвестен) экспортируется как есть, и эмиссию HDRP игра
		/// считает эмиссией: на одном меше с MAT_LAYERS4 она так же умножается на цвет вершин.
		/// </summary>
		[TestCase(true, false, true)]
		[TestCase(true, true, true)]
		[TestCase(false, false, false)]
		public void CountsEmissionOfAMaterialWithoutAUsableOverride(bool emissive, bool withUnknownEntry, bool expectsWarning)
		{
			ModRenderCoreVariant layerBlend = FirstOffered(candidate => candidate.vertexColors == ModRenderCoreVertexColors.LayerBlend, "layer blend");
			RenderCoreMaterialOverride layerBlendOverride = NewOverride(layerBlend.name);
			Material lamp = NewHdrpMaterial("Lamp", emissive ? new Color(r: 2f, g: 1f, b: 0f) : Color.black);
			var overrides = new List<RenderCoreMaterialOverride> { layerBlendOverride };

			if (withUnknownEntry)
			{
				overrides.Add(new RenderCoreMaterialOverride { source = lamp, variant = "MAT_NO_SUCH_VARIANT" });
			}

			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 2, withColors: true), new[] { layerBlendOverride.source, lamp }, overrides.ToArray());

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.AreEqual(expectsWarning, HasIssue(report, MapValidationSeverity.Warning, $"mixes {layerBlend.name} and emissive material 'Lamp' submeshes"));
		}

		[TestCase(0, true)]
		[TestCase(1, true)]
		[TestCase(2, false)]
		[TestCase(3, false)]
		public void WarnsAboutAnEntryBakedForAnOlderRevision(int storedRevision, bool expectsWarning)
		{
			ModRenderCoreVariant offered = FirstOffered(candidate => true, "any");
			ModRenderCoreVariant bumped = WithRevision(offered, revision: 2);
			RenderCoreMaterialOverride materialOverride = NewOverride(offered.name);
			materialOverride.revision = storedRevision;
			GameMarkerData marker = NewMaterialMarker(NewObject("Track"), NewMesh(subMeshCount: 1, withColors: true), materialOverride);
			var report = new MapValidationReport();

			SceneValidator.CheckBakedRevision(report, marker, materialOverride, bumped);

			// Ревизия 0 (запись до появления поля) считается ревизией 1.
			Assert.AreEqual(expectsWarning, HasIssue(report, MapValidationSeverity.Warning,
				$"({offered.name}) was exported from material for revision {Math.Max(1, storedRevision)}, but this SDK has revision 2"));
			Assert.AreEqual(expectsWarning, HasIssue(report, MapValidationSeverity.Warning, "re-run Export from material"));
		}

		[Test]
		public void ChecksTheBakedRevisionOfEveryMarkerEntry()
		{
			ModRenderCoreVariant variant = FirstOffered(candidate => true, "any");
			RenderCoreMaterialOverride materialOverride = NewOverride(variant.name);
			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), materialOverride);

			// Запись без ревизии устарела, только если таблица уже повысила ревизию варианта.
			Assert.AreEqual(variant.revision > 1, HasIssue(Validate(FormatBuild.Binary, root), MapValidationSeverity.Warning, "re-run Export from material"));

			materialOverride.revision = variant.revision;
			Assert.IsFalse(HasIssue(Validate(FormatBuild.Binary, root), MapValidationSeverity.Warning, "re-run Export from material"));
		}

		[Test]
		public void NewMarkerEntriesRecordTheRevisionOfTheSuggestedVariant()
		{
			Material material = NewHdrpMaterial("Wall", Color.black);
			GameObject gameObject = NewObject("Wall", typeof(MeshFilter), typeof(MeshRenderer), typeof(GameMarkerData));
			gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;

			RenderCoreMaterialMarkerSettings settings = RenderCoreMaterialMarkerSettingsDrawer.CreateSettings(gameObject.GetComponent<GameMarkerData>());

			Assert.AreEqual(1, settings.overrides.Count);
			RenderCoreMaterialOverride entry = settings.overrides[0];
			Assert.IsTrue(ModRenderCoreVariants.TryGet(entry.variant, out ModRenderCoreVariant variant), entry.variant);
			// Не 0: запись создана для текущей раскладки варианта.
			Assert.AreEqual(variant.revision, entry.revision);
		}

		private const string FoliageLighting = "gives it foliage lighting";
		private const string VariantMismatch = "is mapped to different render core variants";

		// Растительность без записи в маркере Material не проверяется: такая листва рисуется обычным Cutout, и это решение автора карты.
		[Test]
		public void DoesNotWarnAboutFoliageWithoutAMaterialMarker()
		{
			Material leaves = NewFoliageMaterial("Leaves");
			GameObject root = NewObject("Track");
			NewRenderer("Bush", root, leaves);

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "Leaves"));
		}

		[Test]
		public void AcceptsFoliageCoveredByItsOwnMarker()
		{
			Material leaves = NewFoliageMaterial("Leaves");
			GameObject root = NewObject("Track");
			AddMaterialMarker(NewRenderer("Bush", root, leaves).gameObject, Entry(leaves, FoliageVariant()));

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, FoliageLighting));
		}

		// An entry with an unknown variant is already an error; the foliage hint would only repeat it.
		[Test]
		public void LeavesAFoliageEntryWithAnUnknownVariantToTheVariantError()
		{
			Material leaves = NewFoliageMaterial("Leaves");
			GameObject root = NewObject("Track");
			AddMaterialMarker(NewRenderer("Bush", root, leaves).gameObject, new RenderCoreMaterialOverride { source = leaves, variant = "MAT_NO_SUCH_VARIANT" });

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.IsTrue(HasIssue(report, MapValidationSeverity.Error, "has no supported render core variant ('MAT_NO_SUCH_VARIANT')"));
			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, FoliageLighting));
		}

		// TestMap fir: the LODGroup maps the leaves to a foliage variant, while LOD3 has its own marker with a cutout variant.
		[Test]
		public void WarnsWhenOneLodMapsTheMaterialToAnotherVariant()
		{
			ModRenderCoreVariant foliage = FoliageVariant();
			ModRenderCoreVariant cutout = CutoutVariant();
			Material leaves = NewFoliageMaterial("Leaves");
			GameObject root = NewObject("Track");
			MeshRenderer lod3 = NewRenderer("Fir_LOD3", root, leaves);
			GameObject fir = NewLodGroup("Fir", root, NewRenderer("Fir_LOD0", root, leaves), lod3);
			AddMaterialMarker(fir, Entry(leaves, foliage));
			AddMaterialMarker(lod3.gameObject, Entry(leaves, cutout));

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.IsTrue(HasIssue(report, MapValidationSeverity.Warning,
				$"Material 'Leaves' {VariantMismatch} by Material markers: {foliage.name} on 'Fir'; {cutout.name} on 'Fir_LOD3'."));
			Assert.AreEqual(1, report.Issues.Count(issue => issue.message.Contains(VariantMismatch)));
		}

		[Test]
		public void AcceptsTheSameVariantInEveryMarker()
		{
			ModRenderCoreVariant variant = FirstOffered(candidate => true, "any");
			RenderCoreMaterialOverride first = NewOverride(variant.name);
			var second = new RenderCoreMaterialOverride { source = first.source, variant = variant.name };
			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), first);
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), second);

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, VariantMismatch));
		}

		// Only applied entries count: an unknown variant exports the fallback material and is reported as an error of its own.
		[TestCase("MAT_NO_SUCH_VARIANT", false)]
		[TestCase(null, true)]
		public void ComparesTheVariantsOfAppliedEntriesOnly(string otherVariant, bool expectsWarning)
		{
			ModRenderCoreVariant variant = FirstOffered(candidate => true, "any");
			ModRenderCoreVariant other = FirstOffered(candidate => candidate.name != variant.name, "second");
			RenderCoreMaterialOverride first = NewOverride(variant.name);
			var second = new RenderCoreMaterialOverride { source = first.source, variant = otherVariant ?? other.name };
			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), first);
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), second);

			MapValidationReport report = Validate(FormatBuild.Binary, root);

			Assert.AreEqual(expectsWarning, HasIssue(report, MapValidationSeverity.Warning, $"Material '{first.source.name}' {VariantMismatch}"));
		}

		[Test]
		public void IgnoresVariantMismatchesInWavefront()
		{
			ModRenderCoreVariant variant = FirstOffered(candidate => true, "any");
			ModRenderCoreVariant other = FirstOffered(candidate => candidate.name != variant.name, "second");
			RenderCoreMaterialOverride first = NewOverride(variant.name);
			var second = new RenderCoreMaterialOverride { source = first.source, variant = other.name };
			GameObject root = NewObject("Track");
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), first);
			NewMaterialMarker(root, NewMesh(subMeshCount: 1, withColors: true), second);

			Assert.IsFalse(Has(Validate(FormatBuild.Wavefront, root), SceneValidator.CategoryMarkers, VariantMismatch));
		}

		// A cutout variant keeps the alpha test of vegetation but not its lighting; only foliage-looking materials in Binary builds get the hint,
		// and only as information: the same flags fit fences and grates.
		[TestCase(FormatBuild.Binary, true, false, true)]
		[TestCase(FormatBuild.Binary, true, true, false)]
		[TestCase(FormatBuild.Binary, false, false, false)]
		[TestCase(FormatBuild.Wavefront, true, false, false)]
		public void WarnsAboutFoliageMappedToACutoutVariant(FormatBuild format, bool foliageMaterial, bool foliageVariant, bool expectsWarning)
		{
			ModRenderCoreVariant variant = foliageVariant ? FoliageVariant() : CutoutVariant();
			Material leaves = NewFoliageMaterial("Leaves", doubleSided: foliageMaterial);
			GameObject root = NewObject("Track");
			AddMaterialMarker(NewRenderer("Bush", root, leaves).gameObject, Entry(leaves, variant));

			MapValidationReport report = Validate(format, root);

			Assert.AreEqual(expectsWarning, HasIssue(report, MapValidationSeverity.Info,
				$"'Bush': material 'Leaves' is double-sided and alpha-tested (vegetation?) and is mapped to {variant.name}"));
			Assert.AreEqual(expectsWarning, HasIssue(report, MapValidationSeverity.Info, FoliageLighting));
			Assert.IsFalse(HasIssue(report, MapValidationSeverity.Warning, FoliageLighting));
		}

		private MapValidationReport Validate(FormatBuild format, params GameObject[] roots)
		{
			return Validate(format, roots, new List<string>());
		}

		private MapValidationReport Validate(FormatBuild format, GameObject[] roots, List<string> skipTypes)
		{
			var report = new MapValidationReport();
			SceneValidator.Validate(report, roots, format, FreshRules(), skipTypes);
			return report;
		}

		[TestCase(MapValidationSeverity.Info, LogType.Log, "note")]
		[TestCase(MapValidationSeverity.Warning, LogType.Warning, "warning")]
		// The console summary of a report with errors is a warning; the Map Validation window lists the errors themselves.
		[TestCase(MapValidationSeverity.Error, LogType.Warning, "error")]
		public void ConsoleSummaryUsesTheReportSeverity(MapValidationSeverity severity, LogType logType, string label)
		{
			var report = new MapValidationReport { title = "Preview" };
			report.Add(severity, SceneValidator.CategoryFormat, "Preview component");
			UnityEngine.TestTools.LogAssert.Expect(logType,
				$"Map validation - Preview: 1 {label}. The Map Validation window has the list.");
			report.WriteSummaryToConsole();
		}

		private static bool Has(MapValidationReport report, string category, string fragment)
		{
			return report.Issues.Any(issue =>
				issue.category == category &&
				issue.message.IndexOf(fragment, StringComparison.Ordinal) >= 0);
		}

		private GameObject NewObject(string name, params Type[] components)
		{
			return NewObject(name, null, components);
		}

		private GameObject NewObject(string name, GameObject parent, params Type[] components)
		{
			var gameObject = new GameObject(name, components);

			if (parent != null)
			{
				gameObject.transform.SetParent(parent.transform);
			}
			else
			{
				m_created.Add(gameObject);
			}

			return gameObject;
		}

		private GameMarkerData NewMarker(string name, string head, GameObject parent)
		{
			var gameObject = NewObject(name, parent, typeof(GameMarkerData));
			var marker = gameObject.GetComponent<GameMarkerData>();
			marker.markerData = new MarkerData { head = head };
			return marker;
		}

		private Light NewLight(string name, LightType type, GameObject parent)
		{
			var light = NewObject(name, parent, typeof(Light)).GetComponent<Light>();
			light.type = type;
			return light;
		}

		private static bool HasIssue(MapValidationReport report, MapValidationSeverity severity, string fragment)
		{
			return report.Issues.Any(issue =>
				issue.category == SceneValidator.CategoryMarkers &&
				issue.severity == severity &&
				issue.message.IndexOf(fragment, StringComparison.Ordinal) >= 0);
		}

		private static bool IsDiffuse(ModRenderCoreVariantProperty property)
		{
			return property.source == ModRenderCoreSource.Diffuse;
		}

		/// <summary>Первый вариант зеркала, доступный в маркере; без подходящего тест пропускается.</summary>
		private static ModRenderCoreVariant FirstOffered(Func<ModRenderCoreVariant, bool> predicate, string kind)
		{
			ModRenderCoreVariant variant = ModRenderCoreVariants.Offered.FirstOrDefault(predicate);

			if (variant == null)
			{
				Assert.Ignore($"The render core mirror offers no {kind} variant.");
			}

			return variant;
		}

		/// <summary>Объект с MeshRenderer и маркером Material: по материалу рендерера на каждую запись маркера.</summary>
		private GameMarkerData NewMaterialMarker(GameObject parent, Mesh mesh, params RenderCoreMaterialOverride[] overrides)
		{
			return NewMaterialMarker(parent, mesh, overrides.Select(materialOverride => materialOverride.source).ToArray(), overrides);
		}

		/// <summary>Объект с MeshRenderer с материалами <paramref name="materials"/> и маркером Material с записями <paramref name="overrides"/>.</summary>
		private GameMarkerData NewMaterialMarker(GameObject parent, Mesh mesh, Material[] materials, params RenderCoreMaterialOverride[] overrides)
		{
			GameObject gameObject = NewObject("Marked " + mesh.name, parent, typeof(MeshFilter), typeof(MeshRenderer));
			gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
			gameObject.GetComponent<MeshRenderer>().sharedMaterials = materials;
			return AddMaterialMarker(gameObject, overrides);
		}

		/// <summary>Material marker with entries <paramref name="overrides"/> on an existing object (a renderer or a LODGroup).</summary>
		private static GameMarkerData AddMaterialMarker(GameObject gameObject, params RenderCoreMaterialOverride[] overrides)
		{
			var settings = new RenderCoreMaterialMarkerSettings();
			settings.overrides.AddRange(overrides);

			GameMarkerData marker = gameObject.AddComponent<GameMarkerData>();
			marker.markerData = new MarkerData { head = MarkerData.MaterialHead, param = MarkerData.MaterialHead, value = settings };
			return marker;
		}

		/// <summary>Entry of the current revision of <paramref name="variant"/> for <paramref name="source"/>.</summary>
		private static RenderCoreMaterialOverride Entry(Material source, ModRenderCoreVariant variant)
		{
			return new RenderCoreMaterialOverride { source = source, variant = variant.name, revision = variant.revision };
		}

		/// <summary>Object with a one-triangle MeshRenderer per material.</summary>
		private MeshRenderer NewRenderer(string name, GameObject parent, params Material[] materials)
		{
			GameObject gameObject = NewObject(name, parent, typeof(MeshFilter), typeof(MeshRenderer));
			gameObject.GetComponent<MeshFilter>().sharedMesh = NewMesh(subMeshCount: materials.Length, withColors: true);
			MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
			renderer.sharedMaterials = materials;
			return renderer;
		}

		/// <summary>LODGroup object with one LOD level per renderer, created under <paramref name="parent"/>; the renderers become its children.</summary>
		private GameObject NewLodGroup(string name, GameObject parent, params MeshRenderer[] levels)
		{
			GameObject gameObject = NewObject(name, parent, typeof(LODGroup));
			var lods = new LOD[levels.Length];

			for (int i = 0; i < levels.Length; i++)
			{
				levels[i].transform.SetParent(gameObject.transform);
				lods[i] = new LOD(screenRelativeTransitionHeight: 0.5f / (i + 1), renderers: new Renderer[] { levels[i] });
			}

			gameObject.GetComponent<LODGroup>().SetLODs(lods);
			return gameObject;
		}

		/// <summary>HDRP Lit material with Alpha Clipping and Double-Sided switched by the HDRP floats the baker reads.</summary>
		private Material NewFoliageMaterial(string name, bool alphaClipping = true, bool doubleSided = true)
		{
			Material material = NewHdrpMaterial(name, Color.black);
			material.SetFloat("_AlphaCutoffEnable", alphaClipping ? 1f : 0f);
			material.SetFloat("_DoubleSidedEnable", doubleSided ? 1f : 0f);
			return material;
		}

		private static bool IsFoliageVariant(ModRenderCoreVariant variant)
		{
			return variant.name.StartsWith("MAT_FOLIAGE_", StringComparison.Ordinal);
		}

		private static ModRenderCoreVariant FoliageVariant()
		{
			return FirstOffered(candidate => candidate.renderType == ModRenderCoreRenderType.AlphaTest && IsFoliageVariant(candidate), "foliage");
		}

		private static ModRenderCoreVariant CutoutVariant()
		{
			return FirstOffered(candidate => candidate.renderType == ModRenderCoreRenderType.AlphaTest && !IsFoliageVariant(candidate),
				"non-foliage alpha test");
		}

		private RenderCoreMaterialOverride NewOverride(string variant)
		{
			var material = new Material(Shader.Find("Hidden/InternalErrorShader")) { name = variant + " source" };
			m_assets.Add(material);
			return new RenderCoreMaterialOverride { source = material, variant = variant };
		}

		/// <summary>Материал HDRP Lit с цветом эмиссии <paramref name="emissiveColor"/>; без HDRP в проекте тест пропускается.</summary>
		private Material NewHdrpMaterial(string name, Color emissiveColor)
		{
			Shader shader = Shader.Find("HDRP/Lit");

			if (shader == null)
			{
				Assert.Ignore("HDRP/Lit is not available in this project.");
			}

			var material = new Material(shader) { name = name };
			material.SetColor("_EmissiveColor", emissiveColor);
			m_assets.Add(material);
			return material;
		}

		/// <summary>Копия варианта таблицы с другой ревизией: в текущей таблице все ревизии могут быть равны 1.</summary>
		private static ModRenderCoreVariant WithRevision(ModRenderCoreVariant variant, int revision)
		{
			return new ModRenderCoreVariant(variant.name, variant.id, variant.queue, variant.renderType, variant.keywords, variant.vertexColors,
				variant.requiredStreams, variant.unavailableReason, revision, variant.properties);
		}

		/// <summary>Треугольник на каждый сабмеш, с цветом вершин или без него.</summary>
		private Mesh NewMesh(int subMeshCount, bool withColors)
		{
			var mesh = new Mesh { name = "Mesh" + m_assets.Count };
			mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };

			if (withColors)
			{
				mesh.colors = new[] { Color.red, Color.green, Color.blue };
			}

			mesh.subMeshCount = subMeshCount;

			for (int i = 0; i < subMeshCount; i++)
			{
				mesh.SetTriangles(new[] { 0, 1, 2 }, i);
			}

			m_assets.Add(mesh);
			return mesh;
		}

		private Texture2D NewTexture()
		{
			var texture = new Texture2D(width: 2, height: 2);
			m_assets.Add(texture);
			return texture;
		}
	}
}
