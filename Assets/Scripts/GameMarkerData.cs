using System;
using System.Collections.Generic;
using UnityEngine;

public class GameMarkerData : MonoBehaviour
{
    public MarkerData markerData;
}

[Serializable]
public class MarkerData
{
    public static readonly string[] paramEditor =
    {
        "SpawnPoint",
        "Road/default",
        "Road/asphalt",
        "Road/brick",
        "Road/cobbles",
        "Road/cobbles_big",
        "Road/concrete",
        "Road/dirt",
        "Road/earth",
        "Road/grass",
        "Road/gravel",
        "Road/ice_snowy",
        "Road/kerb_racetrack",
        "Road/liquid_water",
        "Road/metal",
        "Road/metal_grille",
        "Road/pavement",
        "Road/paving_slab",
        "Road/plastic",
        "Road/road_icy",
        "Road/sand",
        "Road/snow",
        "Road/snow_asphalt",
        "Road/snow_cobbles",
        "Road/snow_gravel",
        "Road/wood",
        "Road/wood_plank",
        "Ambient/Crowd",
        "Ambient/Crowd_parking",
        "Ambient/Garage1",
        "Ambient/Garage2",
        "Ambient/Kami",
        "Ambient/Pacific",
        "Ambient/Stadium",
        "Ambient/Thunder",
        "Ambient/Thunder2",
        "Ambient/Track",
        "Ambient/Waterfall",
        "Ambient/Winter",
    };
    
    public string head;
    public string param;
    [SerializeReference] public object value;
    public int index;
    public string lastHeadObject;
    public string templateName;
    public int templateIndex;
    public GameMarkerTemplateConfig templateConfig;
    
    public static readonly Dictionary<string, Func<object>> paramObjectsEditor = new Dictionary<string, Func<object>>()
    {
        {"Road", () => new PhysicMaterialProperties()}
    };
    
    public void Update()
    {
        if (templateConfig == null)
        {
            value = null;
            return; 
        }
        
        if (templateConfig.presets.presets.Length == 1)
        {
            return;
        }
        value = templateConfig.presets.presets[templateIndex].value;
    }
    
    public string GetHead() => head.ToLower();
    public static string GetHeadTarget(string param)
    {
        var group = param.IndexOf('/');
        return group != -1 ? param.Substring(0, group) : param;
    }
}

[Serializable]
public struct PhysicMaterialProperties
{
    public float friction;
	public float rollFriction;
	public float bumpMin;
	public float bumpMax;
	public float bumpScale;
	public float bump2Min;
	public float bump2Max;
	public float bump2Scale;
}

[Serializable]
public struct FMODBunks
{
    public TextAsset[] fmodBunks;
}