using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Plugins.CarX.Modding.Creator.Editor;
using Plugins.CarX.Modding.Creator.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MapUploader.Tests
{
	public class PrimitiveColliderExportTests
	{
		private GameObject source;
		private GameObject target;

		[SetUp]
		public void SetUp()
		{
			source = new GameObject("Source");
			target = new GameObject("Target");
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(source);
			Object.DestroyImmediate(target);
		}

		private LODInfo Collect() => (LODInfo)typeof(SceneFormatCollector)
			.GetMethod("CollectLodInfo", BindingFlags.NonPublic | BindingFlags.Static)
			.Invoke(null, new object[] { source, null });

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(2)]
		public void JsonRoundTripRestoresNativeShapesWithLocalParameters(int direction)
		{
			source.transform.localScale = new Vector3(-2, 3, 4);
			var box = source.AddComponent<BoxCollider>();
			box.center = new Vector3(1, 2, 3);
			box.size = new Vector3(4, 5, 6);
			var sphere = source.AddComponent<SphereCollider>();
			sphere.center = new Vector3(-1, 2, -3);
			sphere.radius = 2.5f;
			var capsule = source.AddComponent<CapsuleCollider>();
			capsule.center = new Vector3(3, 2, 1);
			capsule.radius = 1.25f;
			capsule.height = 6;
			capsule.direction = direction;

			var info = Collect();
			Assert.IsNull(info.meshCollider);
			Assert.IsTrue(info.HasContent);
			var prefab = new PrefabInstance
			{
				mesh = string.Empty, material = string.Empty, collider = string.Empty,
				primitiveColliders = info.primitiveColliders
			};
			var restored = JsonUtility.FromJson<PrefabInstance>(JsonUtility.ToJson(prefab));
			Assert.AreEqual(prefab, restored);
			Assert.AreEqual(prefab.GetHashCode(), restored.GetHashCode());
			target.transform.localScale = source.transform.localScale;
			foreach (var primitive in restored.primitiveColliders)
				primitive.AddTo(target);
			Assert.AreEqual(3, target.GetComponents<Collider>().Length);
			Assert.IsNull(target.GetComponent<MeshCollider>());
			Assert.AreEqual(box.center, target.GetComponent<BoxCollider>().center);
			Assert.AreEqual(box.size, target.GetComponent<BoxCollider>().size);
			Assert.AreEqual(sphere.center, target.GetComponent<SphereCollider>().center);
			Assert.AreEqual(sphere.radius, target.GetComponent<SphereCollider>().radius);
			Assert.AreEqual(capsule.center, target.GetComponent<CapsuleCollider>().center);
			Assert.AreEqual(capsule.radius, target.GetComponent<CapsuleCollider>().radius);
			Assert.AreEqual(capsule.height, target.GetComponent<CapsuleCollider>().height);
			Assert.AreEqual(direction, target.GetComponent<CapsuleCollider>().direction);
		}

		[Test]
		public void MeshColliderDoesNotSuppressPrimitivesAndSkippedCollidersStayExcluded()
		{
			var mesh = new Mesh();
			try
			{
				source.AddComponent<MeshCollider>().sharedMesh = mesh;
				source.AddComponent<BoxCollider>();
				source.AddComponent<SphereCollider>().enabled = false;
				source.AddComponent<CapsuleCollider>().isTrigger = true;
				var info = Collect();
				Assert.AreSame(mesh, info.meshCollider);
				Assert.AreEqual(1, info.primitiveColliders.Length);
				Assert.AreEqual(PrimitiveColliderType.Box, info.primitiveColliders[0].type);
			}
			finally { Object.DestroyImmediate(mesh); }
		}

		[Test]
		public void PrimitiveOnlyObjectsSurviveCollectionAndDifferentSizesRemainDistinct()
		{
			var box = source.AddComponent<BoxCollider>();
			var first = Collect();
			var unityPrefab = new UnityPrefabInstance { lods = new List<LODInfo> { first } };
			Assert.IsFalse(unityPrefab.IsNull());
			box.size = new Vector3(2, 3, 4);
			var provider = new ObjMtlExporterProvider(null);
			var create = typeof(SceneFormatCollector).GetMethod("CreatePrefabInstanceWithPath",
				BindingFlags.NonPublic | BindingFlags.Static, null,
				new[] { typeof(LODInfo), typeof(IModResourcesProvider) }, null);
			var a = (PrefabInstance)create.Invoke(null, new object[] { first, provider });
			var b = (PrefabInstance)create.Invoke(null, new object[] { Collect(), provider });
			Assert.IsNull(a.collider);
			Assert.AreEqual(1, a.primitiveColliders.Length);
			Assert.AreEqual(2, new HashSet<PrefabInstance> { a, b }.Count);
		}

		[Test]
		public void PacksPrimitiveOnlyMapAsMetadataWithoutObjFiles()
		{
			source.AddComponent<BoxCollider>().size = new Vector3(2, 3, 4);
			target.transform.SetParent(source.transform);
			target.AddComponent<SphereCollider>().radius = 2.5f;
			string catalog = Path.Combine(Path.GetTempPath(), "PrimitiveColliderExport-" + Guid.NewGuid().ToString("N"));
			try
			{
				var collector = new SceneFormatCollector(source.transform, "Primitives", null);
				var results = collector.CollectModResults(new EditorCollectionProvider(), ModdingVersion.GetFullVersionFormat());
				results.UploadInCatalog(catalog);
				string json = File.ReadAllText(Directory.GetFiles(Path.Combine(catalog, "prefabs"), "*.json").Single());
				var metadata = JsonUtility.FromJson<PrefabHierarchyMeta>(json);
				Assert.AreEqual(2, metadata.prefabInstances.Count);
				Assert.IsTrue(metadata.prefabInstances.All(p => p.primitiveColliders.Length == 1 && string.IsNullOrEmpty(p.collider)));
				Assert.IsEmpty(Directory.GetFiles(catalog, "*.obj", SearchOption.AllDirectories));
			}
			finally
			{
				if (Directory.Exists(catalog))
					Directory.Delete(catalog, true);
			}
		}

		[Test]
		public void OldMetadataAndEmptyPrimitiveListsHaveTheSameIdentity()
		{
			var old = JsonUtility.FromJson<PrefabInstance>("{\"mesh\":\"mesh/1\",\"collider\":\"mesh/2\"}");
			var empty = old;
			empty.primitiveColliders = Array.Empty<PrimitiveColliderInstance>();
			Assert.AreEqual(old, empty);
			Assert.AreEqual(old.GetHashCode(), empty.GetHashCode());
		}
	}
}
