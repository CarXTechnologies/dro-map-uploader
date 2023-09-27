using Unity.Mathematics;
using UnityEngine;

public class DrawTriggerZoneBehaviour : MonoBehaviour
{
    public FormType form;
    public bool castToFloor = true;
    
    public enum FormType
    {
        Point,
        Box,
        Sphere
    }
    
    private void OnDrawGizmos()
    {
        var thisTrans = transform;
        Vector3 pos = thisTrans.position;
        Vector3 scale = thisTrans.localScale;

        if (castToFloor)
        {
            RayMaxDraw(ref pos, ref scale, Vector3.down);
        }

        thisTrans.position = pos;
        Gizmos.color = Color.green;
        
        switch (form)
        {
            case FormType.Point :
                break;
            case FormType.Box :
                thisTrans.localScale = scale;
                Gizmos.matrix = thisTrans.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                break;
            case FormType.Sphere :
                var lossyScale = thisTrans.lossyScale;
                thisTrans.localScale = Vector3.one * lossyScale.y; 
                Gizmos.matrix = thisTrans.localToWorldMatrix;
                Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
                break;
        }
        
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