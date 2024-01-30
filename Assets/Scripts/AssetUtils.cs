using System.Linq;
using UnityEngine;

public static class AssetUtils
{
    public static T GetDBConfig<T>(string name) where T : ScriptableObject
    {
        var objects = Resources.FindObjectsOfTypeAll<T>();
        var objFinds = objects.FirstOrDefault(nameAsset => nameAsset.name == name);
        return objFinds != default ? objFinds : null;
    }
}