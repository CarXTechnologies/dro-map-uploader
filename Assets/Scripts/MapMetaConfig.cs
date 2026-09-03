using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/MapMetaConfig", fileName = "MapMetaConfig", order = 0)]
public class MapMetaConfig : ScriptableObject
{
	[Lock] public string id;
	public MapMetaConfigValue mapMetaConfigValue;

	public event Action<MapMetaConfigValue> updateValue;

	private void OnValidate()
	{
		updateValue?.Invoke(mapMetaConfigValue);
	}
}
