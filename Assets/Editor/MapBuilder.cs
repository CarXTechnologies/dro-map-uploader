using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameOverlay;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Editor
{
    public class MapBuilder : MonoBehaviour
    {
        private static string assetDir = Application.temporaryCachePath + "/";
        private static string assetBuildPath = assetDir + "Standalone";
        private static string assetBuildPathTemporaryOrigin = assetDir + "StandaloneTemporary";
        private static string assetBuildPathTemporary = assetBuildPathTemporaryOrigin;
        private const string path = "Assets";

        private static List<GameMarkerData> m_cacheDataList = new List<GameMarkerData>();
        private static CacheData m_cacheData;
        public static SteamUGCManager steamUgc;
        private static string m_scenePath;
        private static string m_titleIconPath;
        private static string m_assetPath;
        private static PublishedFileId m_currentFileId;
        private static BuildAssetBundleOptions m_assetBundleOption = BuildAssetBundleOptions.UncompressedAssetBundle;
        private static BuildTarget m_buildTarget = BuildTarget.StandaloneWindows;
        
        [Obsolete("Obsolete")]
        private static void Create()
        {
            if (IsCurrentSceneCheck())
            {
                return;
            }
            
            InitPath();
            ClearCacheScene();
            ClearDirectory(assetBuildPath);
            
            if (CheckAndError())
            {
                return;
            }
            
            ValidateSceneAndMirror();
            CreateBundles(new PublishedFileId());
        }
        
        [Obsolete("Obsolete")]
        private static void CreateAndPublication()
        {
            if (IsCurrentSceneCheck())
            {
                return;
            }
            
            InitPath();
            ClearCacheScene();
            ClearDirectory(assetBuildPath);
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
            steamUgc.SetItemData(MapManagerConfig.Value.mapName, m_titleIconPath, MapManagerConfig.Value.mapDescription);
            EditorCoroutineUtility.StartCoroutine(steamUgc.CreatePublisherItem(item =>
            {
                m_currentFileId = item.FileId;
                EditorUtility.ClearProgressBar();
                CreateBundles(m_currentFileId);
                BuildDataTransition();
                
                EditorUtility.DisplayProgressBar("Upload Publisher Item", String.Empty, 0.75f);
                if (IsSizeValid())
                {
                    return;
                }
                
                EditorCoroutineUtility.StartCoroutine(steamUgc.PublishItemCoroutine(assetBuildPath, PublishCallback), steamUgc);
            }), steamUgc);
        }
        
        [Obsolete("Obsolete")]
        private static async void UpdateExistPublication()
        {
            if (IsCurrentSceneCheck())
            {
                return;
            }
            
            InitPath();
            ClearCacheScene();
            ClearDirectory(assetBuildPath);
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
            BuildDataTransition();
            
            if (IsSizeValid())
            {
                return;
            }
            
            EditorUtility.DisplayProgressBar("Upload Publisher Item", String.Empty, 0.5f);
            steamUgc.SetItemData(MapManagerConfig.Value.mapName, m_titleIconPath, MapManagerConfig.Value.mapDescription);
            EditorCoroutineUtility.StartCoroutine(
                steamUgc.UploadItemCoroutine(assetBuildPath, MapManagerConfig.Value.itemWorkshopId, PublishCallback), steamUgc);
        }

        private static bool PublishCallback(ulong id)
        {
            EditorUtility.ClearProgressBar();
            Directory.Delete(assetBuildPath, true);

            if (id == SteamUGCManager.PUBLISH_ITEM_FAILED_CODE)
            {
                Debug.LogError("Publish failed");
                return false;
            }
                
            MapManagerConfig.instance.mapMetaConfigValue.mapMetaConfigValue.itemWorkshopId = id;
            Debug.Log("Export track id: " + id);
            return true;
        }

        private static bool IsCurrentSceneCheck()
        {
            var currentScene = EditorSceneManager.GetActiveScene().path;
            return MapManagerConfig.instance.targetScene != currentScene && 
                   !EditorUtility.DisplayDialog($"Build scene : {MapManagerConfig.instance.targetScene}", 
                       $"Close and save the current scene : {currentScene}", "Yes", "Cancel");
        }
        
        private static bool CheckAndError()
        {
            if (string.IsNullOrWhiteSpace(MapManagerConfig.Value.mapName))
            {
                Debug.LogError($"Please name your track");
                return true;
            }

            var build = MapManagerConfig.Build;
            if (!MapManagerConfig.Value.mapName.All(char.IsLetter))
            {
                Debug.LogError($"Track name only letters");
                return true;
            }
            
            if (MapManagerConfig.Value.mapName.Length > 128 && MapManagerConfig.instance.uploadSteamName)
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
                Debug.LogError($"Icon more 1mb");
                return true;
            }
            
            if ((float)new FileInfo(m_assetPath + AssetDatabase.GetAssetPath(MapManagerConfig.Value.largeIcon)).Length 
                / ModMapTestTool.BYTES_TO_MEGABYTES > 10f)
            {
                Debug.LogError($"Large icon more 10mb");
                return true;
            }
            
            if (MapManagerConfig.Value.mapDescription.Length > 8000 && MapManagerConfig.instance.uploadSteamDescription)
            {
                Debug.LogError($"Map description must be less than 8000 characters({MapManagerConfig.Value.mapDescription.Length})");
                return true;
            }

            return false;
        }

        private static void ClearDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }
        
        private static void CopyTemporary(string source, string dest, string searchPattern)
        {
            foreach (var file in Directory.GetFiles(source, searchPattern))
            {
                var fileName = file.Substring(source.Length + 1);
                File.Copy(Path.Combine(source, fileName), Path.Combine(dest, fileName), true);
            }
        }
        
        private static void CopyTemporary(string source, string dest)
        {
            foreach (var file in Directory.GetFiles(source))
            {
                var fileName = file.Substring(source.Length + 1);
                File.Copy(Path.Combine(source, fileName), Path.Combine(dest, fileName));
            }
        }

        private static void ClearCacheScene()
        {
            foreach (var file in Directory.GetFiles(path, "*.unity"))
            {
                File.Delete(file);
            }
        }

        public static void InitSteamUGC()
        {
            if (steamUgc == null)
            {
                SteamClient.Init(SteamUGCManager.APP_ID, false);
                steamUgc = new SteamUGCManager();
                EditorApplication.update += steamUgc.Update;
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
            ModMapTestTool.RunTest(MapManagerConfig.instance.targetScene);

            return isError;
        }

        private static void InitPath()
        {
            m_scenePath = path + "/" + MapManagerConfig.Value.mapName + ".unity";
            m_assetPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
            m_titleIconPath = m_assetPath + AssetDatabase.GetAssetPath(MapManagerConfig.Value.icon);
        }
        
        [Obsolete("Obsolete")]
        private static bool ValidateSceneAndMirror()
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(MapManagerConfig.instance.targetScene);
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

            ModMapTestTool.Target = (ValidItemData)ModMapTestTool.Steam.Clone();
            
            if (IsValidate(scene))
            {
                EditorSceneManager.OpenScene(MapManagerConfig.instance.targetScene);
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

            bool noValidComp = false;

            DuplicateValidComponents(root.transform, null, "Garbage", (go, component) =>
            {
                var compType = component.GetType();
                if (!ModMapTestTool.ValidType(component, ModMapTestTool.Target.data))
                {
                    if (ModMapTestTool.ValidType(compType, MapSkipComponentConfig.instance.valid))
                    {
                        return;
                    }

                    noValidComp = true;
                    ModMapTestTool.TryErrorMessage(compType.Name, $"No valid component : {compType.Name}");
                    return;
                }
                
                UnityEditorInternal.ComponentUtility.CopyComponent(component);
                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(go);

                if (compType.Name == nameof(GameMarkerData))
                {
                    var comp = go.GetComponent<GameMarkerData>();
                    comp.markerData.Update();
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
                }

                if (compType.Name == nameof(LODGroup))
                {
                    var groupLods = (component as LODGroup)?.GetLODs();

                    for (int i = 0; i < groupLods.Length; i++)
                    {
                        groupLods[i].renderers = go.transform.FindAllComponent<Renderer>(groupLods[i].renderers);
                    }

                    go.GetComponent<LODGroup>().SetLODs(groupLods);
                }
                
                if (compType.Name == nameof(ReflectionProbe))
                {
                    m_cacheData.reflectionProbe = go.GetComponent<ReflectionProbe>();
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
            
            return noValidComp;
        }

        private static string GetTemporary(TempData name)
        {
            var pathDir = Path.Combine(assetBuildPathTemporary, name + "Temp");
            if (!Directory.Exists(pathDir))
            {
                Directory.CreateDirectory(pathDir);
            }

            return pathDir;
        }
        
        private static void CreateBundles(PublishedFileId publishResult)
        {
            RenameCacheScene(publishResult);
            CreateMapBundle();
            CreateMetaBundle();
        }

        private static void RenameCacheScene(PublishedFileId publishResult)
        {
            var scenePathNew = path + "/" + MapManagerConfig.Value.mapName + publishResult.Value + ".unity";
            AssetDatabase.RenameAsset(m_scenePath, MapManagerConfig.Value.mapName + publishResult.Value);
            m_scenePath = scenePathNew;
        }

        private static string GetCacheName()
        {
            return MapManagerConfig.instance.mapMetaConfigValue.id;
        }
        
        private static void CreateMapBundle()
        {
            var bundleBuilds = CreateBundleArrayDataForOneElement(MapManagerConfig.Value.mapName, m_scenePath);
            BuildPipeline.BuildAssetBundles(GetTemporary(TempData.Map),
                bundleBuilds, m_assetBundleOption, m_buildTarget);
        }

        private static void CreateMetaBundle()
        {
            MapManagerConfig.instance.mapMetaConfigValue.mapMetaConfigValue.compress = MapManagerConfig.Build.compress;
            MapManagerConfig.instance.mapMetaConfigValue.mapMetaConfigValue.platform = MapManagerConfig.Build.platform;
            var bundleBuilds = CreateBundleArrayDataForOneElement(TempData.Meta.ToString().ToLower(), "Assets/Resources/" + MapManagerConfig.instance.name + ".asset");
            BuildPipeline.BuildAssetBundles(GetTemporary(TempData.Meta), 
                bundleBuilds, m_assetBundleOption, m_buildTarget);
        }

        private static void SelectCache()
        {
            assetBuildPathTemporary = assetBuildPathTemporaryOrigin + GetCacheName();
        }
        
        [Obsolete("Obsolete")]
        public static async void BuildCustom(
            TempData target, 
            TempData success, 
            PublishedFileId published,
            CompressBuild compressBuild,
            PlatformBuild platformBuild, 
            Action<string, TempData> callback)
        {
            try
            {
                MapManagerConfig.instance.mapMetaConfigValue.mapMetaConfigValue.itemWorkshopId = published.Value;
                
                switch (compressBuild)
                {
                    case CompressBuild.NoCompress :
                        m_assetBundleOption = BuildAssetBundleOptions.UncompressedAssetBundle;
                        break;
                    case CompressBuild.Compress :
                        m_assetBundleOption = BuildAssetBundleOptions.None;
                        break;
                }
                
                switch (platformBuild)
                {
                    case PlatformBuild.Steam :
                        m_buildTarget = BuildTarget.StandaloneWindows;
                        break;
                }
                
                SelectCache();
                if (target.HasFlag(TempData.Meta))
                {
                    ClearDirectory(GetTemporary(TempData.Meta));
                    InitPath();

                    if (!CheckAndError())
                    {
                        CreateMetaBundle();
                        ClearCacheScene();
                        success |= TempData.Meta;
                    }
                    else
                    {
                        success = (TempData)((int)success & (~(int)TempData.Meta));
                    }
                }

                if (target.HasFlag(TempData.Map))
                {
                    if (!IsCurrentSceneCheck())
                    {
                        ClearDirectory(GetTemporary(TempData.Map));
                        InitPath();

                        if (!ValidateSceneAndMirror())
                        {
                            RenameCacheScene(published);
                            CreateMapBundle();
                            success |= TempData.Map;
                        }
                        else
                        {
                            success = (TempData)((int)success & (~(int)TempData.Map));
                        }
                    }
                    else
                    {
                        success = (TempData)((int)success & (~(int)TempData.Map));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                throw;
            }
            finally
            {
                while (BuildPipeline.isBuildingPlayer)
                {
                    await Task.Delay(100);
                }
                callback?.Invoke(assetBuildPathTemporary, success);
            
                MapManagerConfig.SaveForce();
            }
        }

        public static void CreateNewCommunityFile(Action<PublishResult> callback)
        {
            EditorUtility.DisplayProgressBar("Creating Community File...", string.Empty, 1f);
            EditorCoroutineUtility.StartCoroutine(steamUgc.CreatePublisherItem(result =>
            {
                callback?.Invoke(result);
                EditorUtility.ClearProgressBar();
            }), steamUgc);
        }
        
        public static void UploadCommunityFile(PublishedFileId published, Action<PublishedFileId> callback)
        {
            InitPath();
            ModMapTestTool.Target = (ValidItemData)ModMapTestTool.Steam.Clone();
            if (CheckAndError())
            {
                EditorUtility.ClearProgressBar();
                return;
            }
            
            SelectCache();
            EditorUtility.DisplayProgressBar("Uploading Community File...", string.Empty, 1f);
            ClearDirectory(assetBuildPath);
            BuildDataTransition();
            
            if (IsSizeValid())
            {
                EditorUtility.ClearProgressBar();
                return;
            }
            
            steamUgc.SetItemData(MapManagerConfig.Value.mapName, m_titleIconPath, MapManagerConfig.Value.mapDescription);
            EditorCoroutineUtility.StartCoroutine(steamUgc.UploadItemCoroutine(assetBuildPath, published, 
                publish =>
                {
                    if (!PublishCallback(publish))
                    {
                        return false;
                    }
                    
                    callback?.Invoke(publish);
                    return true;
                }), steamUgc);
        }

        public static string GetSceneName(string path)
        {
            var pos = path.LastIndexOf('/');
            return pos == -1 ? path : path.Substring(pos + 1, path.Length - pos - 7);
        }

        private static void BuildDataTransition()
        {
            ClearDirectory(assetBuildPath);
            CopyTemporary(GetTemporary(TempData.Map), assetBuildPath);
            CopyTemporary(GetTemporary(TempData.Meta), assetBuildPath);
        }

        private static bool IsSizeValid()
        {
            bool notMapSize = ModMapTestTool.IsNotCorrectMapFileSize(MapManagerConfig.Value.mapName, assetBuildPath + "/" + MapManagerConfig.Value.mapName);
            bool notMetaSize = ModMapTestTool.IsNotCorrectMetaFileSize(assetBuildPath + "/" + TempData.Meta.ToString().ToLower());

            if (notMapSize || notMetaSize)
            {
                Directory.Delete(assetBuildPath, true);
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
                    tag = "Untagged",
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

