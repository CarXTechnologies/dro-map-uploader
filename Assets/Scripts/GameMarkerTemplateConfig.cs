using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/GameMarkerTemplateConfig", fileName = "GameMarkerTemplateConfig", order = 0)]
public class GameMarkerTemplateConfig : ScriptableObject
{
	public ListGameMarkerTemplate presets = new();
}

[Serializable]
public class ListGameMarkerTemplate
{
	public MarkerData[] presets = { new() };
	public int selectHead = 0;
	public string templateName = string.Empty;
}