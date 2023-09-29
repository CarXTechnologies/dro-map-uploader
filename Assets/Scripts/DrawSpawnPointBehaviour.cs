using UnityEngine;

public class DrawSpawnPointBehaviour : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawSphere(Vector3.zero, 1f);
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.white;
    }
}