using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct MapMetaConfigValue
{
    public string mapName;
    [TextArea]  public string mapDescription;
    public Texture2D icon;
    public Texture2D largeIcon;

    public PlatformBuild platform;
    public CompressBuild compress;
    
    public ulong itemWorkshopId;

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