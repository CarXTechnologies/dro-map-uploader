using System;
using Steamworks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class GameMarkerData : MonoBehaviour
{
    public MarkerData markerData;
    
}

[Serializable]
public struct MarkerData
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
        "Road/show",
        "Road/show_asphalt",
        "Road/show_cobbles",
        "Road/show_gravel",
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
    [TextArea] public string param;
    public int index;
    
    public string GetHead() => head.ToLower();
}