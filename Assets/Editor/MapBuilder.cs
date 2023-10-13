using System;
using System.Collections.Generic;
using System.IO;
using GameOverlay;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
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
        private static string assetDir = Application.temporaryCachePath + "/";
        private const string path = "Assets";
        private static string assetManifestPath = assetDir + "Standalone";
        private const string meta = "Meta";

        private static List<GameMarkerData> m_cacheDataList = new List<GameMarkerData>();
        private static CacheData m_cacheData;
        private static SteamUGCManager m_steamUgc;
        private static string m_scenePath;
        private static string m_titleIconPath;

        
        [MenuItem("Map/Create")]
        [Obsolete("Obsolete")]
        private static void Create()
        {
            InitPath();
            InitDirectories();
            
            if (CheckAndError())
            {
                return;
            }
            
            ValidateSceneAndMirror();
            CreateBundles(new PublishedFileId());
        }

        [MenuItem("Map/Create and publication")]
        [Obsolete("Obsolete")]
        private static void CreateAndPublication()
        {
            InitPath();
            InitDirectories();
            InitSteamUGC();
            
            if (CheckAndError())
            {
                return;
            }

            if (ValidateSceneAndMirror())
            {
                return;
            }
            
            m_steamUgc.SetItemData(MapManagerConfig.Value.mapName, m_titleIconPath, MapManagerConfig.Value.mapDescription);
            EditorCoroutineUtility.StartCoroutine(m_steamUgc.CreatePublisherItem(item =>
            {
                CreateBundles(item.FileId);
                if (IsSizeValid())
                {
                    return;
                }

                MapManagerConfig.instance.mapMetaConfigValue.mapMetaConfigValue.lastItemWorkshop = item.FileId;
                EditorCoroutineUtility.StartCoroutine(m_steamUgc.PublishItemCoroutine(assetManifestPath, PublishCallback), m_steamUgc);
            }), m_steamUgc);
        }

        [MenuItem("Map/Update exist publication")]
        [Obsolete("Obsolete")]
        private static void UpdateExistPublication()
        {
            InitPath();
            InitDirectories();
            InitSteamUGC();
            
            if (CheckAndError())
            {
                return;
            }
            
            if (ValidateSceneAndMirror())
            {
                return;
            }

            CreateBundles(MapManagerConfig.Value.lastItemWorkshop);
            if (IsSizeValid())
            {
                return;
            }
            
            m_steamUgc.SetItemData(MapManagerConfig.Value.mapName, m_titleIconPath, MapManagerConfig.Value.mapDescription);
            EditorCoroutineUtility.StartCoroutine(
                m_steamUgc.UploadItemCoroutine(assetManifestPath, MapManagerConfig.Value.lastItemWorkshop), m_steamUgc);
        }
        
        private static void PublishCallback(ulong id)
        {
            Directory.Delete(assetManifestPath, true);

            if (id == SteamUGCManager.PUBLISH_ITEM_FAILED_CODE)
            {
                Debug.LogError("Publish failed");
                return;
            }
                
            Debug.Log("Export track id: " + id);
        }

        private static bool CheckAndError()
        {
            if (MapManagerConfig.Value.icon == null)
            {
                Debug.LogError($"Please apply icon config({MapManagerConfig.instance.mapMetaConfigValue.name}) field");
                return true;
            }
            
            if (!MapManagerConfig.Value.largeIcon.isReadable || !MapManagerConfig.Value.icon.isReadable)
            {
                Debug.LogError($"Icon no valid format");
                return true;
            }
            
            
            if ((float)new FileInfo(m_titleIconPath).Length / ModMapTestTool.BYTES_TO_MEGABYTES > 1f)
            {
                Debug.LogError($"IconPreview more 1mb");
                return true;
            }

            return false;
        }
        
        private static void InitDirectories()
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            foreach (var file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }
            
            if (Directory.Exists(assetManifestPath))
            {
                Directory.Delete(assetManifestPath, true);
            }

            Directory.CreateDirectory(assetManifestPath);
        }
        
        private static void InitSteamUGC()
        {
            if (m_steamUgc == null)
            {
                SteamClient.Shutdown();
                SteamClient.Init(SteamUGCManager.APP_ID, false);
                m_steamUgc = new SteamUGCManager();
                EditorApplication.update += m_steamUgc.Update;
            }
        }

        private static bool IsValidate(Scene scene)
        {
            bool isError = false;
            
            ModMapTestTool.errorCallback = (name, error) =>
            {
                Debug.LogError(error);
                isError = true;
            };
            
            ModMapTestTool.Play(MapManagerConfig.Value.mapName)?
                .WithList(ModMapTestTool.Steam)
                .ValidComponents();

            ModMapTestTool.InitTestsEditor(scene);
            ModMapTestTool.RunTest(MapManagerConfig.Value.targetScene);

            return isError;
        }

        private static void InitPath()
        {
            m_scenePath = path + "/" + MapManagerConfig.Value.mapName + ".unity";
            var assetPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            m_titleIconPath = assetPath + AssetDatabase.GetAssetPath(MapManagerConfig.Value.largeIcon);
        }
        
        private static bool ValidateSceneAndMirror()
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(MapManagerConfig.Value.GetTargetScenePath());
            var scene = SceneManager.GetActiveScene();
            var sceneObjects = scene.GetRootGameObjects();
            var root = new GameObject("root");

            for (int i = 0; i < sceneObjects.Length; i++)
            {
                sceneObjects[i].transform.SetParent(root.transform);
            }
            
            var mapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            m_cacheDataList.Clear();
            m_cacheData = new GameObject("CacheData", typeof(CacheData)).GetComponent<CacheData>();

            if (IsValidate(scene))
            {
                EditorSceneManager.OpenScene(MapManagerConfig.Value.GetTargetScenePath());
                try
                {
                    SceneManager.UnloadSceneAsync(mapScene);
                }
                catch
                {
                    // ignored
                }

                DestroyImmediate(root);
                return true;
            }
            
            DuplicateValidComponents(root.transform, null, "Garbage", (go, component) =>
            {
                UnityEditorInternal.ComponentUtility.CopyComponent(component);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);
                
                switch (component.GetType().Name)
                {
                    case nameof(GameMarkerData) :
                        var comp = go.GetComponent<GameMarkerData>();
                        m_cacheDataList.Add(comp);
                        if (comp.markerData.GetHead() == "road")
                        {
                            var road = comp.gameObject;
                            road.isStatic = true;
                            GameObjectUtility.SetStaticEditorFlags(road, 
                                StaticEditorFlags.BatchingStatic |
                                StaticEditorFlags.NavigationStatic | 
                                StaticEditorFlags.OccludeeStatic | 
                                StaticEditorFlags.OccluderStatic | 
                                StaticEditorFlags.ReflectionProbeStatic | 
                                StaticEditorFlags.OffMeshLinkGeneration);
                        }
                        break;
                    case nameof(LODGroup) :
                        var groupLods = (component as LODGroup)?.GetLODs();

                        for (int i = 0; i < groupLods.Length; i++)
                        {
                            groupLods[i].renderers = go.transform.FindAllComponent<Renderer>(groupLods[i].renderers);
                        }
                        
                        go.GetComponent<LODGroup>().SetLODs(groupLods);
                        break;
                }
            });
            
            for (int i = 0; i < sceneObjects.Length; i++)
            {
                sceneObjects[i].transform.SetParent(null);
            }
            
            m_cacheData.gameMarkers = new List<GameMarkerData>(m_cacheDataList.ToArray());
            
            DestroyImmediate(root);
            
            EditorSceneManager.SaveScene(mapScene, m_scenePath);
            SceneManager.UnloadScene(mapScene);

            return false;
        }

        private static void CreateBundles(PublishedFileId publishResult)
        {
            var scenePathNew = path + "/" + MapManagerConfig.Value.mapName + publishResult.Value + ".unity";
            AssetDatabase.RenameAsset(m_scenePath, MapManagerConfig.Value.mapName + publishResult.Value);
            m_scenePath = scenePathNew;
                
            var bundleBuilds = CreateBundleArrayDataForOneElement(MapManagerConfig.Value.mapName, m_scenePath);
            BuildPipeline.BuildAssetBundles(assetManifestPath,
                bundleBuilds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
            
            bundleBuilds = CreateBundleArrayDataForOneElement(meta, "Assets/Resources/" + MapManagerConfig.instance.name + ".asset");
            BuildPipeline.BuildAssetBundles(assetManifestPath, 
                bundleBuilds, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        }

        private static bool IsSizeValid()
        {
            bool notMapSize = ModMapTestTool.IsNotCorrectMapFileSize(MapManagerConfig.Value.mapName, assetManifestPath + "/" + MapManagerConfig.Value.mapName);
            bool notMetaSize = ModMapTestTool.IsNotCorrectMetaFileSize(assetManifestPath + "/" + meta);

            if (notMapSize || notMetaSize)
            {
                Directory.Delete(assetManifestPath, true);
                return true;
            }

            return false;
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
                
                for (var i = 0; i < parent.transform.childCount; i++)
                {
                    var child = parent.transform.GetChild(i);
                    DuplicateValidComponents(child, go.transform, tagGarbage, tryAct);
                }
                
                foreach (var component in allComponents)
                {
                    if (component != null)
                    {
                        tryAct?.Invoke(go, component);
                    }
                }
            }
        }
    }
}

public static class ComponentUtility
{
    public static T[] FindAllComponent<T>(this Transform parent, params Component[] validNames) where T : Component
    {
        var components = new List<T>(parent.childCount);
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            var childChild = child.FindAllComponent<T>();
            if (childChild != null && childChild.Length > 0)
            {
                components.AddRange(childChild);
            }

            var component = child.GetComponent<T>();
            if (component != null)
            {
                bool validName = false;
                foreach (var name in validNames)
                {
                    if (component.transform.name == name.transform.name)
                    {
                        validName = true;
                        break;
                    }
                }

                if (validName)
                {
                    components.Add(component);
                }
            }
        }

        return components.ToArray();
    }
}