using System;
using System.Collections.Generic;
using System.IO;
using GameOverlay;
using Steamworks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Editor
{
    public class MapBuilder : MonoBehaviour
    {
        private const string assetDir = "C:/Program Files (x86)/Steam/steamapps/workshop/content/635260/";
        private const string path = "Assets/map";
        public const string assetManifestPath = assetDir + "Standalone";
        public const string meta = "Meta";

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
           
            EditorSceneManager.OpenScene(MapMetaConfig.Value.GetTargetScenePath());
            var scene = SceneManager.GetActiveScene();
            var sceneObjects = scene.GetRootGameObjects();
            
            var root = new GameObject("root");

            for (int i = 0; i < sceneObjects.Length; i++)
            {
                sceneObjects[i].transform.SetParent(root.transform);
            }
            
            var titleIconPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6)
                                + AssetDatabase.GetAssetPath(MapMetaConfig.Value.largeIcon);
            
            var mapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            m_cacheDataList.Clear();
            m_cacheData = new GameObject("CacheData", typeof(CacheData)).GetComponent<CacheData>();

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
                typeof(MapConfig),
                typeof(CacheData));
            
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                sceneObjects[i].transform.SetParent(null);
            }
            
            m_cacheData.gameMarkers = new List<GameMarkerData>(m_cacheDataList.ToArray());
            
            DestroyImmediate(root);
            
            var scenePath = path + "/" + MapMetaConfig.Value.mapName + ".unity";
                
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
            
            EditorCoroutineUtility.StartCoroutine(steamUgc.CreatePublisherItem(MapMetaConfig.Value.mapName, titleIconPath, item =>
            {
                var scenePathNew = path + "/" + MapMetaConfig.Value.mapName + item.FileId.Value + ".unity";
                AssetDatabase.RenameAsset(scenePath, MapMetaConfig.Value.mapName + item.FileId.Value);
                scenePath = scenePathNew;
                
                var bundleBuilds = CreateBundleArrayDataForOneElement(MapMetaConfig.Value.mapName, scenePath);
                BuildPipeline.BuildAssetBundles(assetManifestPath,
                    bundleBuilds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
            
                bundleBuilds = CreateBundleArrayDataForOneElement(meta, "Assets/Resources/" + MapMetaConfig.instance.name + ".asset");
                BuildPipeline.BuildAssetBundles(assetManifestPath, 
                    bundleBuilds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

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
            
                EditorCoroutineUtility.StartCoroutine(steamUgc.PublishItemCoroutine(MapMetaConfig.Value.mapName,
                assetDir + "Standalone", Callback), steamUgc);
            
                if (ModMapTestTool.RunTest(assetManifestPath, meta, MapMetaConfig.Value.mapName, MapMetaConfig.Value.GetTargetScenePath()))
                {
                    Directory.Delete(assetManifestPath, true);
                }
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

        private static void ValidComponents(Transform parent, Transform root, string tagGarbage, params Type[] components)
        {
            if (!parent.CompareTag(tagGarbage))
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
                    var compType = component.GetType();
                    if (Try(compType, components))
                    {
                        UnityEditorInternal.ComponentUtility.CopyComponent(component);
                        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);
                        
                        if(compType.Name == nameof(GameMarkerData))
                        {
                            m_cacheDataList.Add(go.GetComponent<GameMarkerData>());
                        }
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