using CarX.Modding.Creator.Runtime;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	// Draws Water/Sea game markers in the Scene view: a large translucent plane at the sea level around the marker
	public static class WaterMarkerGizmo
	{
		private const float PlaneSize = 2000f;
		private const float SelectedPlaneSize = 10000f;
		private const float PointRadius = 0.6f;
		private static readonly Color s_planeColor = new(0.2f, 0.5f, 1f, 0.15f);
		private static readonly Color s_edgeColor = new(0.2f, 0.5f, 1f, 0.6f);
		private static readonly Color s_pointColor = new(0.2f, 0.5f, 1f, 0.9f);

		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
		private static void Draw(GameMarkerData marker, GizmoType gizmoType)
		{
			if (marker.MarkerHead != WaterMarkerSettings.MarkerHead || marker.MarkerParam != WaterMarkerSettings.SeaParam)
			{
				return;
			}

			Vector3 position = marker.transform.position;
			float size = (gizmoType & GizmoType.Selected) != 0 ? SelectedPlaneSize : PlaneSize;
			var planeSize = new Vector3(size, 0f, size);

			Gizmos.color = s_pointColor;
			Gizmos.DrawSphere(position, PointRadius);
			Gizmos.color = s_planeColor;
			Gizmos.DrawCube(position, planeSize);
			Gizmos.color = s_edgeColor;
			Gizmos.DrawWireCube(position, planeSize);
		}
	}
}
