using UnityEngine;

public class GameMarker : MonoBehaviour
{
    [SerializeField] private MapPair mapObject;
    
    private void OnValidate()
    {
        mapObject ??= new MapPair();
        
        if (MapObjectConfig.instance == null ||
            MapObjectConfig.Value.standalone == null ||
            MapObjectConfig.Value.standalone.Length <= mapObject.index)
        {
            return;
        }
        
        mapObject.popupValues = MapObjectConfig.Value.standalone;
        transform.name = MapObjectConfig.Value.standalone[mapObject.index];
    }
}