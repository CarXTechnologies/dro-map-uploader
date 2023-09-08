using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/MapObjectConfig", fileName = "MapObjectConfig", order = 0)]
public class MapObjectConfig : SingletonScriptableObject<MapObjectConfig>
{
    public MapObjectConfigValue mapObjectConfigValue;

    public event Action<MapObjectConfigValue> updateValue;

    private void OnValidate()
    {
        updateValue?.Invoke(mapObjectConfigValue);
    }

    public static MapObjectConfigValue Value => instance.mapObjectConfigValue;
}

[Serializable]
public struct MapObjectConfigValue
{
    public string targetBundleMapName;
    public string mapName;
    [TextArea] public string mapDescription;
    public Texture2D icon;
    public Texture2D largeIcon;
    public Texture2D miniMapIcon;
}

public enum TypeGameObject
{
    SpawnPoint,
    Road,
    Config
}