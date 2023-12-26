using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/GameMarkerTemplateConfig", fileName = "GameMarkerTemplateConfig", order = 0)]
public class GameMarkerTemplateConfig : ScriptableObject
{
    public ListGameMarkerTemplate presets;
}

[Serializable]
public class ListGameMarkerTemplate
{
    public MarkerData[] presets = Array.Empty<MarkerData>();
    public int selectHead;
    public string templateName;
}