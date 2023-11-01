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
    [InspectorName("Map Name(only letters, 128 char)")] public string mapName;
    [TextArea] public string mapDescription;
    [InspectorName("Icon(16:9)")] public Texture2D icon;
    [InspectorName("Large icon(16:9)")] public Texture2D largeIcon;
    [HideInInspector]public Texture2D miniMapIcon;

    public ulong itemWorkshopId;
    public bool UploadSteamName;
    public bool UploadSteamDescription;
    public bool UploadSteamPreview;
    
    public string GetTargetScenePath() => $"Assets/MapResources/{targetScene}/{targetScene}.unity";
}

public class InspectorNameAttribute : PropertyAttribute
{
    public string newName { get ; private set; }	
    public InspectorNameAttribute( string name )
    {
        newName = name ;
    }
}