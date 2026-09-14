using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/MapMetaConfig", fileName = "MapMetaConfig", order = 0)]
public class MapMetaConfig : ScriptableObject
{
	[Lock] public string id;
	public MapMetaConfigValue mapMetaConfigValue;
	public Plugins.CarX.Modding.Creator.Runtime.BuildOptimizationSettings optimization = new();

	public string OptimizationKey => optimization != null && optimization.enabled ? JsonUtility.ToJson(optimization.Snapshot()) : string.Empty;
	public event Action<MapMetaConfigValue> updateValue;

	private void OnValidate()
	{
		updateValue?.Invoke(mapMetaConfigValue);
	}
}
