using System;
using System.Collections.Generic;
using System.IO;
using Steamworks.Ugc;
using UnityEditor;
using UnityEngine;

[Flags]
public enum TempData : int
{
    Meta = 1,
    Map = 2,
}
    
public enum PlatformBuild : int
{
    Steam = 0,
    //EpicGames = 1,
    //PS4 = 100,
    //PS5 = 101,
    //XboxOne = 1004,
    //XboxSeriesS = 1005,
    //XboxSeriesX = 1006,
}
    
public enum CompressBuild : int
{
    NoCompress = 0,
    Compress = 10,
}

[CreateAssetMenu(menuName = "Map/MapManagerConfig", fileName = "MapManagerConfig", order = 0)]
public class MapManagerConfig : SingletonScriptableObject<MapManagerConfig>
{
    [InspectorSetting(isLock: true)] public MapMetaConfig mapMetaConfigValue;
    [InspectorSetting(isLock: true)] public List<AttachData> attachingConfigs = new List<AttachData>();
    [InspectorSetting(isLock: true)] public List<BuildData> builds = new List<BuildData>();
    [HideInInspector] public string targetScene;
    [HideInInspector] public bool uploadSteamName;
    [HideInInspector] public bool uploadSteamDescription;
    [HideInInspector] public bool uploadSteamPreview;
    
    [Serializable]
    public class AttachData
    {
        public ulong id;
        public MapMetaConfig metaConfig;
    }
    
    [Serializable]
    public struct BuildData
    {
        public MapMetaConfig config;
        public string path;
        public int buildSuccess;
        public ValidItemData lastValid;
        public MapMetaConfigValue lastMeta;
        public PlatformBuild platform;
        public CompressBuild compress;
        public string targetScene;
        
        public BuildData(MapMetaConfig config, string targetScene, 
            string path, 
            int buildSuccess, 
            ValidItemData lastValid,
            PlatformBuild platform,
            CompressBuild compress)
        {
            this.config = config;
            this.path = path;
            this.buildSuccess = buildSuccess;
            this.lastValid = (ValidItemData)lastValid.Clone();
            lastMeta = config.mapMetaConfigValue;
            this.platform = platform;
            this.compress = compress;
            this.targetScene = targetScene;
        }
    }

    protected override void OnCreate()
    {
        base.OnCreate();
#if UNITY_EDITOR
        EditorUtility.SetDirty(instance);
        Save();
#endif
    }

    public static void ValidBuildsAndAttaching(List<Item> validationBuilds)
    {
        if (validationBuilds == null || validationBuilds.Count < 1)
        {
            return;
        }
        
        for (var index = 0; index < instance.attachingConfigs.Count; index++)
        {
            if (validationBuilds.FindIndex(data => data.Id == instance.attachingConfigs[index].id) == -1)
            {
                instance.attachingConfigs.RemoveAt(index);
            }
        }
        
        for (var index = 0; index < instance.builds.Count; index++)
        {
            if (instance.builds[index].config == null)
            {
                ClearDirectory(instance.builds[index].path);
                instance.builds.RemoveAt(index);
                Save();
            }
        }
        
        SaveForce();
    }
    
    public static void AddBuild(BuildData buildData)
    {
        var index = FindIndexBuild(buildData.config);
        if (index == -1)
        {
            instance.builds.Add(buildData);
            Save();
            return;
        }
        
        instance.builds[index] = buildData;
        Save();
    }

    public static BuildData GetBuildOrEmpty(MapMetaConfig config)
    {
        if (config == null)
        {
            return default;
        }
        
        var result = instance.builds.Find(b => b.config.id == config.id);
        return result;
    }
    
    public static int FindIndexBuild(MapMetaConfig config)
    {
        if (config == null)
        {
            return -1;
        }
        
        var result = instance.builds.FindIndex(b => b.config.id == config.id);
        return result;
    }
    
    public static void ClearBuild(MapMetaConfig config)
    {
        var buildIndex = instance.builds.FindIndex(b => b.config.id == config.id);
        if (buildIndex == -1)
        {
            return;
        }
        
        ClearDirectory(instance.builds[buildIndex].path);
        instance.builds.RemoveAt(buildIndex);
        Save();
    }
    
    private static void ClearDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        Directory.CreateDirectory(path);
    }
    
    public static MapMetaConfigValue Value => instance.mapMetaConfigValue.mapMetaConfigValue;
    public static BuildData Build => GetBuildOrEmpty(instance.mapMetaConfigValue);
    
    public static bool IsAttach(ulong id)
    {
        return instance.attachingConfigs.Exists(data => data.id == id && data.metaConfig != null);
    }
    
    public static AttachData GetAttach(ulong id)
    {
        return instance.attachingConfigs.Find(data => data.id == id);
    }
    
    public static bool TryGetAttach(ulong id, out AttachData attachData)
    {
        attachData = GetAttach(id);
        return attachData != null;
    }
    
    public static bool GetOrAttach(ulong id, out AttachData attachData)
    {
        var result = GetAttach(id);
        attachData = result;
        if (attachData == null)
        {
            attachData = Attach(id, null);
            return true;
        }

        return true;
    }
    
    public static AttachData Attach(ulong id, MapMetaConfig config)
    {
        if (id == 0)
        {
            return null;
        }
        
        if (TryGetAttach(id, out var attachData))
        {
            attachData.id = id;
            attachData.metaConfig = config;
        }
        else
        {
            attachData = new AttachData { id = id, metaConfig = config };
            instance.attachingConfigs.Add(new AttachData { id = id, metaConfig = config });
        }

        Save();
        return attachData;
    }

    public static void Detach(ulong id)
    {
        var index = instance.attachingConfigs.FindIndex(data => data.id == id);
        if (index != -1)
        {
            if (instance.attachingConfigs[index].metaConfig != null)
            {
                instance.attachingConfigs[index].metaConfig.mapMetaConfigValue.itemWorkshopId = 0;
            }

            instance.attachingConfigs[index].metaConfig = null;
        }

        Save();
    }

    public static void Save()
    {
#if UNITY_EDITOR
        AssetDatabase.SaveAssetIfDirty(instance);
        AssetDatabase.Refresh();
#endif
    }
    
    public static void SaveForce()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(instance);
        AssetDatabase.SaveAssetIfDirty(instance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
#endif
    }
}