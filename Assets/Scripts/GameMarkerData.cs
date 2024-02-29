using System;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeReference] public object customValue;
    public int index;
    public string lastHeadObject;
    public string templateName;
    public int templateIndex;
    public GameMarkerTemplateConfig templateConfig;
    
    public static readonly Dictionary<string, Func<string, object>> paramObjectsEditor = new Dictionary<string, Func<string, object>>()
    {
        {"Road", name => 
            AssetUtils.GetDBConfig<SurfaceTemplate>(name.Replace("Road/",string.Empty)).physicMaterial}
    };
    
    public void Update()
    {
        if (templateConfig == null)
        {
            value = null;
            return; 
        }
        
        if (templateConfig.presets.presets.Length < 1)
        {
            return;
        }
        
        var templateFind = templateConfig.presets.presets.FirstOrDefault(template => template.templateName == templateName);
        value = templateFind != default && templateFind.templateName != "Custom" ? templateFind.value : customValue;
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
    [Range(0, 5)] public float friction;
    [Range(0, 1)] public float rollFriction;
    [Range(-1, 0.05f)] public float bumpMin;
    [Range(-0.2f, 1)] public float bumpMax;
    [Range(0, 100)] public float bumpScale;
    [Range(-1, 0)] public float bump2Min;
    [Range(0, 1)] public float bump2Max;
    [Range(-1, 100)] public float bump2Scale;
}