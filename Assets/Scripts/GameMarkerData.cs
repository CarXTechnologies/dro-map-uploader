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
        "Road",
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