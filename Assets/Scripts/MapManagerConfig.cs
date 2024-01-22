using System;
using System.Collections.Generic;
using Steamworks.Ugc;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/MapManagerConfig", fileName = "MapManagerConfig", order = 0)]
public class MapManagerConfig : SingletonScriptableObject<MapManagerConfig>
{
    public MapMetaConfig mapMetaConfigValue;
    public List<AttachData> attachingConfigs = new List<AttachData>();
    public List<Item> fetchResultListItems = new List<Item>();
    
    [Serializable]
    public class AttachData
    {
        public ulong id;
        public MapMetaConfig metaConfig;
        public Texture2D image;
        public bool imageDownloading;
        public int buildSuccess;
        public ValidItemData lastValid;
    }
    
    public static MapMetaConfigValue Value => instance.mapMetaConfigValue.mapMeta;

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
            instance.attachingConfigs.RemoveAt(index);
        }
    }

    public static void Save()
    {
#if UNITY_EDITOR
        AssetDatabase.SaveAssetIfDirty(instance);
#endif
    }
}