using System.IO;
using System.Linq;
using CarX.Modding.Creator.Editor;
using CarX.Modding.Creator.Runtime;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	// Minimap inspector: generates the 3D miniature and its minimap image before the build and shows the miniature in the Scene view
	[CustomEditor(typeof(Minimap))]
	public class MinimapEditor : UnityEditor.Editor
	{
		private const string ShowMiniaturePref = "MapUploader.Minimap.ShowMiniature";
		private const string GarbageTag = "Garbage";
		// The preview floats over the map so the map geometry does not hide it, meters
		private const float PreviewLift = 30f;
		// Bounds smaller than this were never set: the generation frames the miniature itself
		private const float MinBoundsSize = 10f;
		private const float BoundsMargin = 1.1f;

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
			EditorGUILayout.HelpBox("Generate 3D preview builds the world map miniature from the scene into a mesh asset next to the scene and " +
				"draws it from above into a minimap image, which replaces Textures > Element 0. Export uses the mesh in Miniature Mesh " +
				"and the image in Textures: keep the generated ones or set your own.", MessageType.Info);

			// The generation restores the import settings the running build has just changed for export
			using (new EditorGUI.DisabledScope(MapBuilder.IsBuilding))
			{
				if (GUILayout.Button("Generate 3D preview"))
				{
					Generate(minimap);
				}
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
			if (MapBuilder.IsBuilding)
			{
				Debug.LogWarning("[Miniature] The map is being built: generate the 3D preview after the build.");
				return;
			}

			var scene = minimap.gameObject.scene;
			Transform[] roots = scene.GetRootGameObjects().Select(root => root.transform).ToArray();
			MiniatureMeta miniature;

			// The miniature reads the triangles: Read/Write is enabled for the generation only
			try
			{
				RigidbodyExporter.EnsureMeshReadability(roots);
				miniature = MiniatureCollector.Collect(scene.name, ModdingVersion.GetFullVersionFormat(), roots, IsGarbage);
			}
			finally
			{
				ExportImporterSettings.RestoreAll();
			}

			if (miniature == null)
			{
				EditorUtility.DisplayDialog("3D preview", "The scene has no geometry for a miniature.", "OK");
				return;
			}

			string directory = string.IsNullOrEmpty(scene.path) ? "Assets" : Path.GetDirectoryName(scene.path).Replace('\\', '/');
			Mesh mesh = SaveMesh(miniature, $"{directory}/{scene.name}_Miniature.asset", $"{scene.name}_Miniature");

			var serializedMinimap = new SerializedObject(minimap);
			FrameUnsetBounds(serializedMinimap, miniature, minimap.name);
			serializedMinimap.FindProperty("m_miniatureMesh").objectReferenceValue = mesh;

			Vector2 boundsCenter = serializedMinimap.FindProperty("m_boundsCenter").vector2Value;
			Vector2 boundsSize = serializedMinimap.FindProperty("m_boundsSize").vector2Value;
			Texture2D image = SaveImage(miniature, boundsCenter, boundsSize, $"{directory}/{scene.name}_MiniatureMap.png");

			SerializedProperty textures = serializedMinimap.FindProperty("m_textures");

			if (textures.arraySize == 0)
			{
				textures.arraySize = 1;
			}

			textures.GetArrayElementAtIndex(0).FindPropertyRelative("mainTexture").objectReferenceValue = image;
			serializedMinimap.ApplyModifiedProperties();

			IsPreviewShown = true;
			SceneView.RepaintAll();
		}

		// Regenerating keeps the asset (and the references to it), only its data changes
		private static Mesh SaveMesh(MiniatureMeta miniature, string path, string name)
		{
			Mesh mesh = MiniatureMeshUtility.ToMesh(miniature, name);
			var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

			if (existing != null)
			{
				EditorUtility.CopySerialized(mesh, existing);
				DestroyImmediate(mesh);
				EditorUtility.SetDirty(existing);
				AssetDatabase.SaveAssets();

				return existing;
			}

			AssetDatabase.CreateAsset(mesh, path);
			AssetDatabase.SaveAssets();

			return mesh;
		}

		private static Texture2D SaveImage(MiniatureMeta miniature, Vector2 boundsCenter, Vector2 boundsSize, string path)
		{
			Texture2D rendered = MiniatureTextureRenderer.Render(miniature, boundsCenter, boundsSize);
			File.WriteAllBytes(path, rendered.EncodeToPNG());
			DestroyImmediate(rendered);
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

			// The image keeps its own size and its transparent background
			if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
				(importer.npotScale != TextureImporterNPOTScale.None || !importer.alphaIsTransparency || importer.maxTextureSize < MiniatureTextureRenderer.MaxSize))
			{
				importer.npotScale = TextureImporterNPOTScale.None;
				importer.alphaIsTransparency = true;
				importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, MiniatureTextureRenderer.MaxSize);
				importer.SaveAndReimport();
			}

			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}

		// A minimap without bounds collapses to a point in the game: frame the roads and objects, or the terrain when there are none
		private static void FrameUnsetBounds(SerializedObject serializedMinimap, MiniatureMeta miniature, string minimapName)
		{
			SerializedProperty size = serializedMinimap.FindProperty("m_boundsSize");

			if (size.vector2Value.x >= MinBoundsSize && size.vector2Value.y >= MinBoundsSize)
			{
				return;
			}

			MiniatureLayer[] layers = miniature.layers.Where(layer => layer.kind != MiniatureLayerKind.Terrain && layer.vertices.Length > 0).ToArray();

			if (layers.Length == 0)
			{
				layers = miniature.layers.Where(layer => layer.vertices.Length > 0).ToArray();
			}

			Vector3[] vertices = layers.SelectMany(layer => layer.vertices).ToArray();
			var min = new Vector2(vertices.Min(v => v.x), vertices.Min(v => v.z));
			var max = new Vector2(vertices.Max(v => v.x), vertices.Max(v => v.z));
			float side = Mathf.Max(max.x - min.x, max.y - min.y) * BoundsMargin;

			// Lock Size would copy the object scale back into the bounds
			serializedMinimap.FindProperty("m_lockSize").boolValue = false;
			serializedMinimap.FindProperty("m_boundsCenter").vector2Value = (min + max) * 0.5f;
			size.vector2Value = new Vector2(side, side);

			Debug.Log($"[Miniature] '{minimapName}': Bounds Size was not set, the bounds now frame the miniature ({side:F0} m).", serializedMinimap.targetObject);
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

			for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
			{
				Gizmos.color = (MiniatureLayerKind)submesh switch
				{
					MiniatureLayerKind.Ground => MiniatureTextureRenderer.GroundColor,
					MiniatureLayerKind.Terrain => MiniatureTextureRenderer.TerrainColor,
					_ => MiniatureTextureRenderer.GetObjectColor(0f),
				};
				Gizmos.DrawMesh(mesh, submesh, position, Quaternion.identity, Vector3.one);
			}
		}
	}
}
