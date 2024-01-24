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

    public override bool Equals(object obj)
    {
        if (obj is MapMetaConfigValue value)
        {
            return value.icon == icon &&
                   value.largeIcon == largeIcon &&
                   value.mapDescription == mapDescription &&
                   value.mapName == mapName &&
                   value.targetScene == targetScene;
        }

        return false;
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