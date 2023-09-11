using UnityEngine;

public class MapConfig : MonoBehaviour
{
    public MapMetaConfigValue mapMetaConfigValue;
    public MapMetaConfig mapMetaConfig;

    private void OnValidate()
    {
        if (mapMetaConfig == null)
        {
            return;
        }
        
        mapMetaConfig.updateValue -= OnUpdateValue;
        mapMetaConfig.updateValue += OnUpdateValue;
    }

    private void OnUpdateValue(MapMetaConfigValue value)
    {
        mapMetaConfigValue = value;
    }
}
