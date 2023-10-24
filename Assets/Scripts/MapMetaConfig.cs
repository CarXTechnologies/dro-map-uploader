using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/MapMetaConfig", fileName = "MapMetaConfig", order = 0)]
public class MapMetaConfig : ScriptableObject
{
    public MapMetaConfigValue mapMetaConfigValue;

    public event Action<MapMetaConfigValue> updateValue;

    private void OnValidate()
    {
        updateValue?.Invoke(mapMetaConfigValue);
    }
}

[Serializable]
public struct MapMetaConfigValue
{
    public string targetScene;
    public string mapName;
    [TextArea] public string mapDescription;
    public Texture2D icon;
    public Texture2D largeIcon;
    [HideInInspector]public Texture2D miniMapIcon;

    public ulong itemWorkshopId;
    public bool UploadSteamName;
    public bool UploadSteamDescription;
    public bool UploadSteamPreview;
    
    public string GetTargetScenePath() => $"Assets/MapResources/{targetScene}/{targetScene}.unity";
}