using System;
using System.Collections.Generic;
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
        private static string assetDir = Application.temporaryCachePath + "/635260/";
        private const string path = "Assets";
        private static string assetManifestPath = assetDir + "Standalone";
        private const string meta = "Meta";

        private static List<GameMarkerData> m_cacheDataList = new List<GameMarkerData>();
        private static CacheData m_cacheData;
        
        [MenuItem("Map/Create Map")]
        [Obsolete("Obsolete")]
        private static void CreateAsset()
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
           
            EditorSceneManager.OpenScene(MapManagerConfig.Value.GetTargetScenePath());
            var scene = SceneManager.GetActiveScene();
            var sceneObjects = scene.GetRootGameObjects();
            
            var root = new GameObject("root");

            for (int i = 0; i < sceneObjects.Length; i++)
            {
                sceneObjects[i].transform.SetParent(root.transform);
            }
            
            var titleIconPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6)
                                + AssetDatabase.GetAssetPath(MapManagerConfig.Value.largeIcon);
            
            var mapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            m_cacheDataList.Clear();
            m_cacheData = new GameObject("CacheData", typeof(CacheData)).GetComponent<CacheData>();

            bool isError = false;
            
            ModMapTestTool.errorCallback = (name, error) =>
            {
                Debug.LogError(error);
                isError = true;
            };
            
            ModMapTestTool.Play(MapManagerConfig.Value.mapName)?
                .With((typeof(Transform), 0, 1000))
                .With((typeof(MeshCollider), 0, 1000))
                .With((typeof(BoxCollider), 0, 1000))
                .With((typeof(SphereCollider), 0, 1000))
                .With((typeof(GameMarkerData), 0, 1000))
                .With((typeof(MeshRenderer), 0, 1000))
                .With((typeof(MeshFilter), 0, 1000))
                .With((typeof(Light), 0, 1000))
                .With((typeof(HDAdditionalLightData), 0, 1000))
                .With((typeof(Volume), 0, 1000))
                .With((typeof(MapConfig), 0, 1000))
                .With((typeof(CacheData), 0, 1000))
                .With((typeof(ReflectionProbe), 0, 1000))
                .ValidComponents();

            ModMapTestTool.InitTestsEditor(scene);
            ModMapTestTool.RunTest(MapManagerConfig.Value.targetScene);
            
            if (isError)
            {
                for (int i = 0; i < sceneObjects.Length; i++)
                {
                    sceneObjects[i].transform.SetParent(null);
                }
                SceneManager.UnloadSceneAsync(mapScene);
                DestroyImmediate(root);
                return;
            }
            
            DuplicateValidComponents(root.transform, null, "Garbage", (go, component) =>
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(component);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);

                if (component.GetType().Name == nameof(GameMarkerData))
                {
                    m_cacheDataList.Add(go.GetComponent<GameMarkerData>());
                }
            });
            
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                sceneObjects[i].transform.SetParent(null);
            }
            
            m_cacheData.gameMarkers = new List<GameMarkerData>(m_cacheDataList.ToArray());
            
            DestroyImmediate(root);
            
            var scenePath = path + "/" + MapManagerConfig.Value.mapName + ".unity";
                
            foreach (var file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }
                
            EditorSceneManager.SaveScene(mapScene, scenePath);
            SceneManager.UnloadScene(mapScene);

            if (Directory.Exists(assetManifestPath))
            {
                Directory.Delete(assetManifestPath, true);
            }

            Directory.CreateDirectory(assetManifestPath);
            
            SteamClient.Shutdown();
            SteamClient.Init(SteamUGCManager.APP_ID, false);
            var steamUgc = new SteamUGCManager();
            EditorApplication.update += steamUgc.Update;
            
            EditorCoroutineUtility.StartCoroutine(steamUgc.CreatePublisherItem(MapManagerConfig.Value.mapName, titleIconPath, item =>
            {
                var scenePathNew = path + "/" + MapManagerConfig.Value.mapName + item.FileId.Value + ".unity";
                AssetDatabase.RenameAsset(scenePath, MapManagerConfig.Value.mapName + item.FileId.Value);
                scenePath = scenePathNew;
                
                var bundleBuilds = CreateBundleArrayDataForOneElement(MapManagerConfig.Value.mapName, scenePath);
                BuildPipeline.BuildAssetBundles(assetManifestPath,
                    bundleBuilds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
            
                bundleBuilds = CreateBundleArrayDataForOneElement(meta, "Assets/Resources/" + MapManagerConfig.instance.name + ".asset");
                BuildPipeline.BuildAssetBundles(assetManifestPath, 
                    bundleBuilds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
                
                
                bool notMapSize = ModMapTestTool.IsNotCorrectMapFileSize(MapManagerConfig.Value.mapName, assetManifestPath + "/" + MapManagerConfig.Value.mapName);
                bool notMetaSize = ModMapTestTool.IsNotCorrectMetaFileSize(assetManifestPath + "/" + meta);

                if (notMapSize || notMetaSize)
                {
                    Directory.Delete(assetManifestPath, true);
                    return;
                }
                
                void Callback(ulong id)
                {
                    Directory.Delete(assetManifestPath, true);

                    if (id == SteamUGCManager.PUBLISH_ITEM_FAILED_CODE)
                    {
                        Debug.LogError("Publish failed");
                        return;
                    }
                
                    Debug.Log("Export track id: " + id);
                }
            
                EditorCoroutineUtility.StartCoroutine(steamUgc.PublishItemCoroutine(assetManifestPath, Callback), steamUgc);
            }), steamUgc);
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

        private static void DuplicateValidComponents(Transform parent, Transform root, string tagGarbage, Action<GameObject, Component> tryAct)
        {
            if (string.IsNullOrEmpty(tagGarbage) || !parent.CompareTag(tagGarbage))
            {
                var allComponents = parent.GetComponents(typeof(Component));
                var o = parent.gameObject;
                
                var go = new GameObject(parent.transform.name)
                {
                    transform =
                    {
                        parent = root,
                        localPosition = parent.localPosition,
                        localRotation = parent.localRotation,
                        localScale = parent.localScale
                    },
                    tag = o.tag,
                    isStatic = o.isStatic,
                    layer = o.layer
                };

                foreach (var component in allComponents)
                {
                    tryAct?.Invoke(go, component);
                }

                for (var i = 0; i < parent.transform.childCount; i++)
                {
                    var child = parent.transform.GetChild(i);
                    DuplicateValidComponents(child, go.transform, tagGarbage, tryAct);
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