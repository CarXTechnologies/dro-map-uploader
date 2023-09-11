using System;
using System.IO;
using GameOverlay;
using Steamworks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Editor
{
    public class MapBuilder : MonoBehaviour
    {
        const string assetDir = "C:\\Program Files (x86)\\Steam\\steamapps\\workshop\\content\\635260\\";
        
        [MenuItem("Map/Create Map")]
        [Obsolete("Obsolete")]
        private static void TryCreateAsset()
        {
            /*var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter
            {
                testMode = TestMode.PlayMode
            };
            testRunnerApi.RegisterCallbacks(new TestCallbacks());
            testRunnerApi.Execute(new ExecutionSettings(filter));*/
            CreateAsset();
        }
        
        /*
        private class TestCallbacks : IErrorCallbacks
        {
            public void OnError(string message)
            {
                Debug.Log(message);
            }
 
            public void RunFinished(ITestResultAdaptor result)
            {
                Debug.Log("Tests finished");
            }
 
            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log("Tests started");
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                Debug.Log("Tests started");
            }

            public void TestStarted(ITestAdaptor test)
            {
                Debug.Log("Tests started");
            }
        }
        */
        
        [Obsolete("Obsolete")]
        private static void CreateAsset()
        {
            const string path = "Assets/map";
            const string assetManifestPath = assetDir + "Standalone";
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
           
            EditorSceneManager.OpenScene("Assets/" + MapMetaConfig.Value.targetScene + ".unity");
            var scene = SceneManager.GetActiveScene();
            var sceneObjects = scene.GetRootGameObjects();
            
            var root = new GameObject("root");
            
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                sceneObjects[i].transform.SetParent(root.transform);
            }
            
            var mapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            ValidComponents(root.transform, null, "Garbage",
                typeof(Transform), 
                typeof(MeshCollider), 
                typeof(BoxCollider),
                typeof(TerrainCollider),
                typeof(SphereCollider),
                typeof(GameMarkerData),
                typeof(MeshRenderer),
                typeof(MeshFilter),
                typeof(Light),
                typeof(HDAdditionalLightData),
                typeof(Volume),
                typeof(MapConfig));
            
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                sceneObjects[i].transform.SetParent(null);
            }
            DestroyImmediate(root);

            var scenePath = path + "/" + MapMetaConfig.Value.mapName + ".unity";
            
            EditorSceneManager.SaveScene(mapScene, scenePath);
            
            var bundleBuilds = CreateBundleArrayDataForOneElement(MapMetaConfig.Value.mapName, scenePath);
            
            SceneManager.UnloadScene(mapScene);

            if (Directory.Exists(assetManifestPath))
            {
                Directory.Delete(assetManifestPath, true);
                Directory.CreateDirectory(assetManifestPath);
            }
            else
            {
                Directory.CreateDirectory(assetManifestPath);
            }
            
            BuildPipeline.BuildAssetBundles(assetManifestPath, bundleBuilds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
            
            bundleBuilds = CreateBundleArrayDataForOneElement("Meta", "Assets/Resources/" + MapMetaConfig.instance.name + ".asset");
            BuildPipeline.BuildAssetBundles(assetManifestPath, bundleBuilds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        }

        private static AssetBundleBuild[] CreateBundleArrayDataForOneElement(string bundleName, string path)
        {
            var bundleBuilds = new AssetBundleBuild[1];
            bundleBuilds[0].assetBundleName = bundleName;
            bundleBuilds[0].assetNames = new[] { path };
            AssetImporter.GetAtPath(bundleBuilds[0].assetNames[0]).assetBundleName = bundleBuilds[0].assetBundleName;
            AssetDatabase.RemoveUnusedAssetBundleNames();

            return bundleBuilds;
        }

        [MenuItem("Map/Upload Map To Steam")]
        private static void UploadBundle()
        {
            SteamClient.Shutdown();
            SteamClient.Init(SteamUGCManager.APP_ID, false);
			
            void Callback(ulong id)
            {
                Directory.Delete(assetDir + "Standalone", true);
                
                if (id == SteamUGCManager.PUBLISH_ITEM_FAILED_CODE)
                {
                    Debug.LogError("Publish failed");
                    return;
                }
                
                Debug.Log("Export track id: " + id);
            }

            var steamUgc = new SteamUGCManager();
            EditorApplication.update += steamUgc.Update;

            var titleIconPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6)
                                + AssetDatabase.GetAssetPath(MapMetaConfig.Value.largeIcon);
            
            EditorCoroutineUtility.StartCoroutine(
                steamUgc.PublishItemCoroutine(MapMetaConfig.Value.mapName, 
                    assetDir + "Standalone", titleIconPath, Callback), steamUgc);
        }

        private static void ValidComponents(Transform parent, Transform root, string tagGarbage, params Type[] components)
        {
            if (!parent.CompareTag(tagGarbage))
            {
                var allComponents = parent.GetComponents(typeof(Component));
                var go = new GameObject(parent.transform.name);
                go.transform.SetPositionAndRotation(parent.position, parent.rotation);
                go.transform.localScale = parent.localScale;
                go.transform.SetParent(root);
                go.tag = parent.tag;
                var o = parent.gameObject;
                go.layer = o.layer;
                go.isStatic = o.isStatic;
                
                foreach (var component in allComponents)
                {
                    if (Try(component.GetType(), components))
                    {
                        UnityEditorInternal.ComponentUtility.CopyComponent(component);
                        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);
                    }
                }

                for (var i = 0; i < parent.transform.childCount; i++)
                {
                    var child = parent.transform.GetChild(i);
                    ValidComponents(child, go.transform, tagGarbage, components);
                }
            }
        }

        private static bool Try(Type type, params Type[] types)
        {
            if (type == null)
            {
                return true;
            }
            
            var tryComp = false;
            foreach (var typeTry in types)
            {
                if (type.Name == typeTry.Name)
                {
                    tryComp = true;
                    break;
                }
            }

            return tryComp;
        }
    }
}