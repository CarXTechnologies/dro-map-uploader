using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
    
    public void SaveForce()
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssetIfDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
    }
}