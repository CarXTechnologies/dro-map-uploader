using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/MapSkipComponentConfig", fileName = "MapSkipComponentConfig", order = 0)]
public class MapSkipComponentConfig : SingletonScriptableObject<MapSkipComponentConfig>
{
    public List<string> valid = new List<string>();
}
