using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameOverlay;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        private static string m_assetPath;
        private static PublishedFileId m_currentFileId;

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
            
            EditorUtility.DisplayProgressBar("Create Publisher Item", String.Empty, 0.5f);
            m_steamUgc.SetItemData(MapManagerConfig.Value.mapName, m_titleIconPath, MapManagerConfig.Value.mapDescription);
            EditorCoroutineUtility.StartCoroutine(m_steamUgc.CreatePublisherItem(item =>
            {
                m_currentFileId = item.FileId;
                EditorUtility.ClearProgressBar();
                CreateBundles(m_currentFileId);
                
                EditorUtility.DisplayProgressBar("Upload Publisher Item", String.Empty, 0.75f);
                if (IsSizeValid())
                {
                    return;
                }
                
                EditorCoroutineUtility.StartCoroutine(m_steamUgc.PublishItemCoroutine(assetManifestPath, PublishCallback), m_steamUgc);
            }), m_steamUgc);
        }

        [MenuItem("Map/Update exist publication")]
        [Obsolete("Obsolete")]
        private static async void UpdateExistPublication()
        {
            InitPath();
            InitDirectories();
            InitSteamUGC();
            
            var task = Item.GetAsync(MapManagerConfig.Value.itemWorkshopId);;
            await task;

            if (task.Result != null && task.Result.Value.Result != Result.OK)
            {
                Debug.LogError("Workshop error : " + task.Result.Value.Result);
                return;
            }

            if (CheckAndError())
            {
                return;
            }
            
            if (ValidateSceneAndMirror())
            {
                return;
            }

            CreateBundles(MapManagerConfig.Value.itemWorkshopId);
            if (IsSizeValid())
            {
                return;
            }
            
            EditorUtility.DisplayProgressBar("Upload Publisher Item", String.Empty, 0.5f);
            m_steamUgc.SetItemData(MapManagerConfig.Value.mapName, m_titleIconPath, MapManagerConfig.Value.mapDescription);
            EditorCoroutineUtility.StartCoroutine(
                m_steamUgc.UploadItemCoroutine(assetManifestPath, MapManagerConfig.Value.itemWorkshopId, PublishCallback), m_steamUgc);
        }

        private static void PublishCallback(ulong id)
        {
            EditorUtility.ClearProgressBar();
            Directory.Delete(assetManifestPath, true);

            if (id == SteamUGCManager.PUBLISH_ITEM_FAILED_CODE)
            {
                Debug.LogError("Publish failed");
                return;
            }
                
            MapManagerConfig.instance.mapMetaConfigValue.mapMetaConfigValue.itemWorkshopId = id;
            Debug.Log("Export track id: " + id);
        }

        private static bool CheckAndError()
        {
            if (string.IsNullOrWhiteSpace(MapManagerConfig.Value.mapName))
            {
                Debug.LogError($"Please name your track");
                return true;
            }
            
            if (!MapManagerConfig.Value.mapName.All(char.IsLetter))
            {
                Debug.LogError($"Track name only letters");
                return true;
            }
            
            if (MapManagerConfig.Value.mapName.Length > 128 && MapManagerConfig.Value.UploadSteamName)
            {
                Debug.LogError($"Length name more 128 symbols");
                return true;
            }
            
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
                Debug.LogError($"Large icon more 1mb");
                return true;
            }
            
            if ((float)new FileInfo(m_assetPath + AssetDatabase.GetAssetPath(MapManagerConfig.Value.icon)).Length 
                / ModMapTestTool.BYTES_TO_MEGABYTES > 1f)
            {
                Debug.LogError($"Icon more 1mb");
                return true;
            }
            
            if (MapManagerConfig.Value.mapDescription.Length > 8000 && MapManagerConfig.Value.UploadSteamDescription)
            {
                Debug.LogError($"Map description must be less than 8000 characters({MapManagerConfig.Value.mapDescription.Length})");
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
                EditorUtility.ClearProgressBar();
                if (m_currentFileId != 0)
                {
                    SteamUGC.DeleteFileAsync(m_currentFileId);
                    m_currentFileId = 0;
                }

                isError = true;
            };
            
            ModMapTestTool.Play(MapManagerConfig.Value.mapName)?
                .WithList(ModMapTestTool.Target.data)
                .ValidComponents();

            ModMapTestTool.InitTestsEditor(scene);
            ModMapTestTool.RunTest(MapManagerConfig.Value.targetScene);

            return isError;
        }

        private static void InitPath()
        {
            m_scenePath = path + "/" + MapManagerConfig.Value.mapName + ".unity";
            m_assetPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            m_titleIconPath = m_assetPath + AssetDatabase.GetAssetPath(MapManagerConfig.Value.largeIcon);
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
                var compType = component.GetType();
                if (!ModMapTestTool.ValidType(compType, ModMapTestTool.Target.data, false))
                {
                    if (ModMapTestTool.ValidType(compType, MapSkipComponentConfig.instance.valid))
                    {
                        return;
                    }
                    ModMapTestTool.TryErrorMessage(compType.Name, $"No valid component : {compType.Name}");
                    return;
                }
                
                UnityEditorInternal.ComponentUtility.CopyComponent(component);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);
                
                switch (compType.Name)
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
                bundleBuilds, BuildAssetBundleOptions.UncompressedAssetBundle, BuildTarget.StandaloneWindows);
            
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