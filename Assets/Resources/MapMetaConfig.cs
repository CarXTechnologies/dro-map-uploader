using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Map/MapMetaConfig", fileName = "MapMetaConfig", order = 0)]
public class MapMetaConfig : SingletonScriptableObject<MapMetaConfig>
{
    public MapMetaConfigValue mapMetaConfigValue;

    public event Action<MapMetaConfigValue> updateValue;

    private void OnValidate()
    {
        updateValue?.Invoke(mapMetaConfigValue);
    }

    public static MapMetaConfigValue Value => instance.mapMetaConfigValue;
}

[Serializable]
public struct MapMetaConfigValue
{
    public string targetScene;
    public string mapName;
    [TextArea] public string mapDescription;
    public Texture2D icon;
    public Texture2D largeIcon;
    public Texture2D miniMapIcon;

    public string GetTargetScenePath() => "Assets/" + targetScene + ".unity";
}

public enum TypeGameObject
{
    SpawnPoint,
    Road,
    Config
}