using Plugins.CarX.Modding.Creator.Runtime;
using UnityEngine;

public class GameMarkerData : MonoBehaviour, IMarkerDataSource
{
	public MarkerData markerData;

	public string MarkerHead => markerData?.head;
	public string MarkerParam => markerData?.param;
}