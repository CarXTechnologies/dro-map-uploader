using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/GameMarkerTemplateConfig", fileName = "GameMarkerTemplateConfig", order = 0)]
public class GameMarkerTemplateConfig : ScriptableObject
{
    public ListGameMarkerTemplate presets = new ListGameMarkerTemplate();
}

[Serializable]
public class ListGameMarkerTemplate
{
    public MarkerData[] presets = {new MarkerData()};
    public int selectHead = 0;
    public string templateName = String.Empty;
}