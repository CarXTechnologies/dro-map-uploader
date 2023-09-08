using Unity.Mathematics;
using UnityEngine;

public class DrawTriggerZoneBehaviour : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Vector3 pos = transform.position;
        Vector3 scale = transform.localScale;

        RayMaxDraw(ref pos, ref scale, Vector3.down);

        transform.position = pos;
        transform.localScale = scale;
        
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
    }

    private void RayMaxDraw(ref Vector3 pos, ref Vector3 scale, Vector3 direction)
    {
        if (Physics.Raycast(new Ray(pos, direction), out var raycastHit))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(pos, raycastHit.distance * direction);
            pos += (Vector3)((float3)(raycastHit.distance * direction) - (scale * (float3)direction) / 2);
        }
    }
}