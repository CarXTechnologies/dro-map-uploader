using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Map/MapMetaConfig", fileName = "MapMetaConfig", order = 0)]
public class MapMetaConfig : ScriptableObject
{
    [Identifier] public string id;
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
    [HideInInspector] public Texture2D miniMapIcon;

    public ulong itemWorkshopId;
    public bool UploadSteamName;
    public bool UploadSteamDescription;
    public bool UploadSteamPreview;
    
    public string GetTargetScenePath() => $"Assets/MapResources/{targetScene}/{targetScene}.unity";
}

[Serializable]
public struct MapMetaBuildConfigValue
{
    [InspectorName("Map Name(only letters, 128 char)")] public string mapName;
    [TextArea] public string mapDescription;
    [InspectorName("Icon(16:9)")] public Texture2D icon;
    [InspectorName("Large icon(16:9)")] public Texture2D largeIcon;

    public MapMetaBuildConfigValue(string mapName, string mapDescription, Texture2D icon, Texture2D largeIcon)
    {
        this.mapName = mapName;
        this.mapDescription = mapDescription;
        this.icon = icon;
        this.largeIcon = largeIcon;
    }
}

public class InspectorNameAttribute : PropertyAttribute
{
    public string newName { get ; private set; }	
    public InspectorNameAttribute( string name )
    {
        newName = name ;
    }
}