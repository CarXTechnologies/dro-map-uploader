using CarX.Modding.Creator.Runtime;
using UnityEditor;

namespace Editor
{
	// Draws ClipZone game markers in the Scene view: the zones have no mesh of their own
	public static class ClipZoneMarkerGizmo
	{
		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
		private static void Draw(GameMarkerData marker, GizmoType gizmoType)
		{
			if (marker.MarkerHead == ClipZoneMarkerSettings.MarkerHead && marker.MarkerData is ClipZoneMarkerSettings settings)
			{
				ClipZoneGizmos.Draw(marker.transform, settings, (gizmoType & GizmoType.Selected) != 0);
			}
		}
	}
}
