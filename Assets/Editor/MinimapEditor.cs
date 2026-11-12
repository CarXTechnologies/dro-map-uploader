using System.IO;
using System.Linq;
using CarX.Modding.Creator.Editor;
using CarX.Modding.Creator.Runtime;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	// Minimap inspector: generates the 3D miniature into a mesh asset before the build and shows it in the Scene view
	[CustomEditor(typeof(Minimap))]
	public class MinimapEditor : UnityEditor.Editor
	{
		private const string ShowMiniaturePref = "MapUploader.Minimap.ShowMiniature";
		private const string GarbageTag = "Garbage";
		// The preview floats over the map so the map geometry does not hide it, meters
		private const float PreviewLift = 30f;

		// The game colors of the miniature layers (linear there), by submesh
		private static readonly Color[] s_previewColors =
		{
			new Color(0.075f, 0.082f, 0.09f).gamma,
			new Color(0.13f, 0.13f, 0.13f).gamma,
			new Color(0.045f, 0.05f, 0.062f).gamma,
		};

		private static bool IsPreviewShown
		{
			get => EditorPrefs.GetBool(ShowMiniaturePref, true);
			set => EditorPrefs.SetBool(ShowMiniaturePref, value);
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var minimap = (Minimap)target;

			if (minimap.WorldMapPreview != Minimap.WorldMapPreviewMode.Generated3D)
			{
				return;
			}

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("Generate 3D preview builds the world map miniature from the scene into a mesh asset next to the scene. " +
				"Export uses the mesh in Miniature Mesh: keep the generated one or set your own.", MessageType.Info);

			if (GUILayout.Button("Generate 3D preview"))
			{
				Generate(minimap);
			}

			EditorGUI.BeginChangeCheck();
			bool isShown = EditorGUILayout.Toggle("Show in Scene view", IsPreviewShown);

			if (EditorGUI.EndChangeCheck())
			{
				IsPreviewShown = isShown;
				SceneView.RepaintAll();
			}
		}

		private static void Generate(Minimap minimap)
		{
			var scene = minimap.gameObject.scene;
			Transform[] roots = scene.GetRootGameObjects().Select(root => root.transform).ToArray();
			MiniatureMeta miniature = MiniatureCollector.Collect(scene.name, ModdingVersion.GetFullVersionFormat(), roots, IsGarbage,
				MapBuilder.CreateMiniatureRoadFilter(minimap));

			if (miniature == null)
			{
				EditorUtility.DisplayDialog("3D preview", "The scene has no geometry for a miniature.", "OK");
				return;
			}

			string directory = string.IsNullOrEmpty(scene.path) ? "Assets" : Path.GetDirectoryName(scene.path);
			string path = $"{directory}/{scene.name}_Miniature.asset".Replace('\\', '/');
			Mesh mesh = MiniatureMeshUtility.ToMesh(miniature, $"{scene.name}_Miniature");
			var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

			// Regenerating keeps the asset (and the references to it), only its data changes
			if (existing != null)
			{
				EditorUtility.CopySerialized(mesh, existing);
				DestroyImmediate(mesh);
				mesh = existing;
				EditorUtility.SetDirty(mesh);
			}
			else
			{
				AssetDatabase.CreateAsset(mesh, path);
			}

			AssetDatabase.SaveAssets();

			Undo.RecordObject(minimap, "Generate 3D preview");
			minimap.MiniatureMesh = mesh;
			EditorUtility.SetDirty(minimap);
			IsPreviewShown = true;
			SceneView.RepaintAll();
		}

		private static bool IsGarbage(Transform transform)
		{
			for (Transform t = transform; t != null; t = t.parent)
			{
				if (t.CompareTag(GarbageTag))
				{
					return true;
				}
			}

			return false;
		}

		[DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
		private static void DrawPreview(Minimap minimap, GizmoType gizmoType)
		{
			Mesh mesh = minimap.MiniatureMesh;

			if (!IsPreviewShown || mesh == null || minimap.WorldMapPreview != Minimap.WorldMapPreviewMode.Generated3D)
			{
				return;
			}

			Vector3 position = Vector3.up * (mesh.bounds.size.y + PreviewLift);

			for (int submesh = 0; submesh < Mathf.Min(mesh.subMeshCount, s_previewColors.Length); submesh++)
			{
				Gizmos.color = s_previewColors[submesh];
				Gizmos.DrawMesh(mesh, submesh, position, Quaternion.identity, Vector3.one);
			}
		}
	}
}
