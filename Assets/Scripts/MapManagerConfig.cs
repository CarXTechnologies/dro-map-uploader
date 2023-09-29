using UnityEngine;

[CreateAssetMenu(menuName = "Map/MapManagerConfig", fileName = "MapManagerConfig", order = 0)]
public class MapManagerConfig : SingletonScriptableObject<MapManagerConfig>
{
    public MapMetaConfig mapMetaConfigValue;
    public static MapMetaConfigValue Value => instance.mapMetaConfigValue.mapMetaConfigValue;
}