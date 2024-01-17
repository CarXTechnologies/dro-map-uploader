using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/MapManagerConfig", fileName = "MapManagerConfig", order = 0)]
public class MapManagerConfig : SingletonScriptableObject<MapManagerConfig>
{
    public MapMetaConfig mapMetaConfigValue;
    public List<AttachData> attachingConfigs = new List<AttachData>();
    
    [Serializable]
    public class AttachData
    {
        public ulong id;
        public MapMetaConfig metaConfig;
        public int buildSuccess;
        public ValidItemData lastValid;
    }
    
    public static MapMetaConfigValue Value => instance.mapMetaConfigValue.mapMeta;

    public static bool IsAttach(ulong id)
    {
        return instance.attachingConfigs.Exists(data => data.id == id);
    }
    
    public static AttachData GetAttach(ulong id)
    {
        return instance.attachingConfigs.Find(data => data.id == id);
    }
    
    public static void Attach(ulong id, MapMetaConfig config)
    {
        instance.attachingConfigs.Add(new AttachData{id = id, metaConfig = config});
        Save();
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