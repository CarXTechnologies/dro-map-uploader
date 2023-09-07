using UnityEngine;

public class MapConfig : MonoBehaviour
{
    public MapObjectConfigValue mapObjectConfigValue;
    public MapObjectConfig mapObjectConfig;

    private void OnValidate()
    {
        if (mapObjectConfig == null)
        {
            return;
        }
        
        mapObjectConfig.updateValue -= OnUpdateValue;
        mapObjectConfig.updateValue += OnUpdateValue;
    }

    private void OnUpdateValue(MapObjectConfigValue value)
    {
        mapObjectConfigValue = value;
    }
}
