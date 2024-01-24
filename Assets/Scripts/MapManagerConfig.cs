using System;
using System.Collections.Generic;
using System.IO;
using Steamworks.Ugc;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/MapManagerConfig", fileName = "MapManagerConfig", order = 0)]
public class MapManagerConfig : SingletonScriptableObject<MapManagerConfig>
{
    public MapMetaConfig mapMetaConfigValue;
    public List<AttachData> attachingConfigs = new List<AttachData>();
    public List<BuildData> builds = new List<BuildData>();
    
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
        public MapMetaBuildConfigValue lastMeta;

        public BuildData(MapMetaConfig config, string path, int buildSuccess, ValidItemData lastValid)
        {
            this.config = config;
            this.path = path;
            this.buildSuccess = buildSuccess;
            this.lastValid = lastValid;
            lastMeta = default;
            lastMeta = GetBuildValue();
        }
        
        public MapMetaBuildConfigValue GetBuildValue()
        {
            return new MapMetaBuildConfigValue(
                config.mapMetaConfigValue.mapName, 
                config.mapMetaConfigValue.mapDescription, 
                config.mapMetaConfigValue.icon,
                config.mapMetaConfigValue.largeIcon);
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
        instance.builds.Add(buildData);
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
        
        if (attachData.metaConfig != null)
        {
            attachData.metaConfig.mapMetaConfigValue.itemWorkshopId = id;
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