using UnityEngine;

public class DrawZoneBehaviour : MonoBehaviour
{
    [SerializeField] private FormType m_form;
    [SerializeField] private bool m_isEmpty;
    [SerializeField] private bool m_castToFloor = true;
    [SerializeField] private Color m_color = Color.green;
    
    private enum FormType
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

        if (m_castToFloor)
        {
            RayMaxDraw(ref pos, ref scale, Vector3.down);
        }

        thisTrans.position = pos;
        Gizmos.color = m_color;
        
        switch (m_form)
        {
            case FormType.Point :
                break;
            case FormType.Box :
                thisTrans.localScale = scale;
                Gizmos.matrix = thisTrans.localToWorldMatrix;
                if (m_isEmpty)
                {
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                }
                else
                {
                    Gizmos.DrawCube(Vector3.zero, Vector3.one);
                }

                break;
            case FormType.Sphere :
                var lossyScale = thisTrans.lossyScale;
                thisTrans.localScale = Vector3.one * lossyScale.y; 
                Gizmos.matrix = thisTrans.localToWorldMatrix;
                if (m_isEmpty)
                {
                    Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
                }
                else
                {
                    Gizmos.DrawSphere(Vector3.zero, 0.5f);
                }

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
            pos += (raycastHit.distance * direction - new Vector3(scale.x * direction.x, scale.y * direction.y, scale.z * direction.z) / 2);
        }
    }
}