using System;
using UnityEditor;

namespace Editor
{
	public class MapMetaConfigIdStamper : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
			Stamp(importedAssets);
			Stamp(movedAssets);
		}

		[InitializeOnLoadMethod]
		private static void StampAll()
		{
			EditorApplication.delayCall += () =>
			{
				foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(MapMetaConfig)}"))
				{
					Stamp(AssetDatabase.GUIDToAssetPath(guid));
				}
			};
		}

		private static void Stamp(string[] assetPaths)
		{
			if (assetPaths == null)
			{
				return;
			}

			foreach (var assetPath in assetPaths)
			{
				Stamp(assetPath);
			}
		}

		private static void Stamp(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}

			var config = AssetDatabase.LoadAssetAtPath<MapMetaConfig>(assetPath);

			if (config == null)
			{
				return;
			}

			var guid = AssetDatabase.AssetPathToGUID(assetPath);

			if (string.IsNullOrEmpty(guid) || config.id == guid)
			{
				return;
			}

			config.id = guid;
			EditorUtility.SetDirty(config);
		}
	}
}
