using System;
using UnityEngine;

public class GameMarkerData : MonoBehaviour
{
    public MarkerData markerData;

    private void OnValidate()
    {
        markerData.head = markerData.typeGameMaker.ToString();
        transform.name = markerData.head;
    }
}

[Serializable]
public struct MarkerData
{
    [HideInInspector] public string head;
    public TypeGameMaker typeGameMaker;
    [TextArea] public string param;

    public string GetHead() => typeGameMaker.ToString().ToLower();
}

public enum TypeGameMaker
{
    SpawnPoint,
    Road
}

