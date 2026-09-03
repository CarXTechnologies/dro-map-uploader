using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Editor.Validation;
using NUnit.Framework;
using UnityEngine;

namespace MapUploader.Tests
{
	public class SceneValidatorTests
	{
		private readonly List<GameObject> m_created = new();

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

			m_created.Clear();
		}

		[Test]
		public void ReportsAMissingSpawnPoint()
		{
			var root = NewObject("Track");

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "no SpawnPoint marker"));
		}

		[Test]
		public void AcceptsASingleSpawnPoint()
		{
			var root = NewObject("Track");
			NewMarker("Start", "SpawnPoint", root);

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "no SpawnPoint marker"));
		}

		[Test]
		public void RejectsSeveralSpawnPointsOnDro1()
		{
			var root = NewObject("Track");
			NewMarker("StartA", "SpawnPoint", root);
			NewMarker("StartB", "SpawnPoint", root);

			var report = Validate(FormatBuild.dro1, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "dro1 supports exactly one SpawnPoint"));
		}

		[Test]
		public void AllowsSeveralSpawnPointsOnDro2()
		{
			var root = NewObject("Track");
			NewMarker("StartA", "SpawnPoint", root);
			NewMarker("StartB", "SpawnPoint", root);

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "dro1 supports exactly one SpawnPoint"));
			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "are all named"));
		}

		[Test]
		public void WarnsAboutSpawnPointsSharingAName()
		{
			var root = NewObject("Track");
			NewMarker("Start", "SpawnPoint", root);
			NewMarker("Start", "SpawnPoint", root);

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "are all named 'Start'"));
		}

		[Test]
		public void ReportsARoadMarkerWithNoCollider()
		{
			var root = NewObject("Track");
			NewMarker("Asphalt", "Road", root);

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "marked as Road but has no Collider"));
		}

		[Test]
		public void AcceptsARoadMarkerWithAColliderOnAChild()
		{
			var root = NewObject("Track");
			var road = NewMarker("Asphalt", "Road", root);
			NewObject("Surface", road.gameObject, typeof(BoxCollider));

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryMarkers, "marked as Road but has no Collider"));
		}

		[Test]
		public void ReportsAMarkerWithNoTypeSelected()
		{
			var root = NewObject("Track");
			NewMarker("Unset", string.Empty, root);

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryMarkers, "no type selected"));
		}

		[Test]
		public void WarnsAboutContentDro2CannotExport()
		{
			var root = NewObject("Track");
			NewObject("Crate", root, typeof(BoxCollider));

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryFormat, "'BoxCollider' x1"));
		}

		[Test]
		public void SaysNothingAboutTheSameContentOnDro1()
		{
			var root = NewObject("Track");
			NewObject("Crate", root, typeof(BoxCollider));

			var report = Validate(FormatBuild.dro1, root);

			Assert.IsFalse(report.Issues.Any(issue => issue.category == SceneValidator.CategoryFormat));
		}

		[Test]
		public void ReportsAnUnsupportedComponent()
		{
			var root = NewObject("Track");
			NewObject("Speaker", root, typeof(AudioSource));

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryComponents, "'AudioSource' on 'Speaker'"));
		}

		[Test]
		public void SkipsComponentsOnTheSkipList()
		{
			var root = NewObject("Track");
			NewObject("Speaker", root, typeof(AudioSource));

			var report = Validate(FormatBuild.dro2, new[] { root }, new List<string> { nameof(AudioSource) });

			Assert.IsFalse(Has(report, SceneValidator.CategoryComponents, "'AudioSource' on 'Speaker'"));
		}

		[Test]
		public void SkipsAnythingTaggedGarbage()
		{
			var root = NewObject("Track");
			var scratch = NewObject("Scratch", root, typeof(AudioSource));
			scratch.tag = "Garbage";

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryComponents, "'AudioSource' on 'Scratch'"));
		}

		[Test]
		public void ReportsMoreThanOneDirectionalLight()
		{
			var root = NewObject("Track");
			NewLight("SunA", LightType.Directional, root);
			NewLight("SunB", LightType.Directional, root);

			var report = Validate(FormatBuild.dro1, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryLighting, "2 Directional Lights"));
		}

		[Test]
		public void WarnsAboutLightTypesDro2CannotExport()
		{
			var root = NewObject("Track");
			NewLight("Sun", LightType.Directional, root);

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryLighting, "dro2 exports Point and Spot lights only"));
		}

		[Test]
		public void AcceptsPointAndSpotLightsOnDro2()
		{
			var root = NewObject("Track");
			NewLight("Lamp", LightType.Point, root);
			NewLight("Spot", LightType.Spot, root);

			var report = Validate(FormatBuild.dro2, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryLighting, "dro2 exports Point and Spot lights only"));
		}

		[Test]
		public void ReportsAConcaveColliderDrivenByADynamicRigidbody()
		{
			var root = NewObject("Track");
			var prop = NewObject("Barrel", root, typeof(MeshCollider), typeof(Rigidbody));
			prop.GetComponent<MeshCollider>().convex = false;
			prop.GetComponent<Rigidbody>().isKinematic = false;

			var report = Validate(FormatBuild.dro1, root);

			Assert.IsTrue(Has(report, SceneValidator.CategoryPhysics, "non-convex MeshCollider driven by a non-kinematic Rigidbody"));
		}

		[Test]
		public void AcceptsAConcaveColliderOnAKinematicRigidbody()
		{
			var root = NewObject("Track");
			var prop = NewObject("Barrel", root, typeof(MeshCollider), typeof(Rigidbody));
			prop.GetComponent<Rigidbody>().isKinematic = true;

			var report = Validate(FormatBuild.dro1, root);

			Assert.IsFalse(Has(report, SceneValidator.CategoryPhysics, "non-convex MeshCollider driven by a non-kinematic Rigidbody"));
		}

		[Test]
		public void ReportsAMinimapThatWasNeverSetUp()
		{
			var root = NewObject("Track");
			NewObject("Minimap", root, typeof(Minimap));

			var report = Validate(FormatBuild.dro2, root);

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

			var report = Validate(FormatBuild.dro2, root);

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
			SceneValidator.Validate(report, new[] { root }, FormatBuild.dro2, FreshRules(), new List<string>(), root.transform);

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
			SceneValidator.Validate(new MapValidationReport(), new[] { root }, FormatBuild.dro1, rules, new List<string>());

			var boxes = rules.data.First(item => item.type == nameof(BoxCollider));
			Assert.AreEqual(2, boxes.current);
		}

		private static ValidItemData FreshRules()
		{
			return ModMapTestTool.ComponentRules.CloneWithLimits(4096f, 24f);
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
	}
}
