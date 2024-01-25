using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct MapMetaConfigValue
{
    [InspectorSetting("Map Name(only letters, 128 char)")] public string mapName;
    [TextArea] public string mapDescription;
    [InspectorSetting("Icon(16:9)")] public Texture2D icon;
    [InspectorSetting("Large icon(16:9)")] public Texture2D largeIcon;

    public PlatformBuild platform;
    public CompressBuild compress;
    
    [HideInInspector] public ulong itemWorkshopId;

    public override bool Equals(object obj)
    {
        if (obj is MapMetaConfigValue value)
        {
            return value.icon == icon &&
                   value.largeIcon == largeIcon &&
                   value.mapDescription == mapDescription &&
                   value.mapName == mapName && 
                   value.platform == platform &&
                   value.compress == compress;
        }

        return false;
    }
}