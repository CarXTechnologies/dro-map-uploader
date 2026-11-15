using CarX.Modding.Creator.Runtime;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	// Draws Sound game markers in the Scene view: a point, and for a selected positional sound its min and max distance
	public static class SoundMarkerGizmo
	{
		private const float PointRadius = 0.4f;
		private static readonly Color s_pointColor = new(1f, 0.75f, 0.2f, 0.9f);
		private static readonly Color s_ambienceColor = new(0.4f, 0.8f, 1f, 0.9f);
		private static readonly Color s_minDistanceColor = new(1f, 0.75f, 0.2f, 0.8f);
		private static readonly Color s_maxDistanceColor = new(1f, 0.75f, 0.2f, 0.3f);

		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
		private static void Draw(GameMarkerData marker, GizmoType gizmoType)
		{
			if (marker.MarkerHead != SoundMarkerSettings.MarkerHead || marker.MarkerData is not SoundMarkerSettings settings)
			{
				return;
			}

			Vector3 position = marker.transform.position;
			bool isAmbience = settings.Kind(marker.MarkerParam) == SoundEmitterKind.Ambience;

			Gizmos.color = isAmbience ? s_ambienceColor : s_pointColor;
			Gizmos.DrawSphere(position, PointRadius);

			if (isAmbience || (gizmoType & GizmoType.Selected) == 0)
			{
				return;
			}

			Gizmos.color = s_minDistanceColor;
			Gizmos.DrawWireSphere(position, settings.minDistance);
			Gizmos.color = s_maxDistanceColor;
			Gizmos.DrawWireSphere(position, settings.maxDistance);
		}
	}
}
