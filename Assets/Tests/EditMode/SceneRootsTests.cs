using System.Linq;
using NUnit.Framework;
using Plugins.CarX.Modding.Creator.Editor;
using Plugins.CarX.Modding.Creator.Runtime;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapUploader.Tests
{
    public class SceneRootsTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void CatalogExportsMultipleRootsWithoutChangingSource(bool binary)
        {
            var previous = SceneManager.GetActiveScene();
            var source = EditorSceneManager.NewPreviewScene();
            try
            {
                var first = new GameObject("Road", typeof(BoxCollider));
                SceneManager.MoveGameObjectToScene(first, source);
                first.transform.SetPositionAndRotation(new Vector3(8, 2, -4), Quaternion.Euler(0, 31, 0));
                first.transform.localScale = new Vector3(-2, 3, 4);
                var child = new GameObject("Child");
                child.transform.SetParent(first.transform, false);
                child.transform.localPosition = new Vector3(1, 2, 3);
                var second = new GameObject("Scenery");
                SceneManager.MoveGameObjectToScene(second, source);
                second.transform.position = new Vector3(-9, 1, 5);
                var world = child.transform.localToWorldMatrix;
                var sceneCount = SceneManager.sceneCount;
                var wasDirty = source.isDirty;
                var result = new SceneFormatCollector(source.GetRootGameObjects().Select(r => r.transform),
                    "TestRoots", "Garbage").CollectModResults(new EditorCollectionProvider(binary), ModdingVersion.GetFullVersionFormat());
                Assert.IsTrue(result.success);
                Assert.AreEqual(sceneCount, SceneManager.sceneCount);
                Assert.AreEqual(previous, SceneManager.GetActiveScene());
                Assert.IsNull(first.transform.parent);
                Assert.AreEqual(world, child.transform.localToWorldMatrix);
                Assert.AreEqual(new Vector3(-2, 3, 4), first.transform.localScale);
                Assert.AreEqual(new Vector3(-9, 1, 5), second.transform.position);
                Assert.AreEqual(new[] {first, second}, source.GetRootGameObjects());
                Assert.AreEqual(wasDirty, source.isDirty);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(source);
            }
        }
    }
}
