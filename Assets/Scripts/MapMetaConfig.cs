using System;
using UnityEngine;

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