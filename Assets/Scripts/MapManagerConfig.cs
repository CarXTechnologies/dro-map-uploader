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

    [Serializable]
    public class AttachData
    {
        public ulong id;
        public MapMetaConfig metaConfig;
#if UNITY_EDITOR
        public int buildSuccess
        {
            get => metaConfig == null ? 0 : metaConfig.mapMeta.buildSuccess;
            set
            {
                if (metaConfig == null)
                {
                    return;
                }

                metaConfig.mapMeta.buildSuccess = value;
            }
        }

        public ValidItemData lastValid
        {
            get => metaConfig == null ? default : metaConfig.mapMeta.lastValid;
            set
            {
                if (metaConfig == null)
                {
                    return;
                }

                metaConfig.mapMeta.lastValid = value;
            }
        }
#endif
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
            attachData.metaConfig.mapMeta.itemWorkshopId = id;
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
                instance.attachingConfigs[index].metaConfig.mapMeta.itemWorkshopId = 0;
            }

            instance.attachingConfigs[index].metaConfig = null;
        }

        Save();
    }

    public static void Save()
    {
#if UNITY_EDITOR
        AssetDatabase.SaveAssetIfDirty(instance);
#endif
    }
}