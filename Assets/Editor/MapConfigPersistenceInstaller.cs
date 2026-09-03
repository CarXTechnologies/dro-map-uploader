using UnityEditor;

namespace Editor
{
	[InitializeOnLoad]
	internal static class MapConfigPersistenceInstaller
	{
		static MapConfigPersistenceInstaller()
		{
			MapConfigPersistence.markDirty = EditorUtility.SetDirty;

			MapConfigPersistence.save = target => AssetDatabase.SaveAssetIfDirty(target);

			MapConfigPersistence.saveForce = target =>
			{
				EditorUtility.SetDirty(target);
				AssetDatabase.SaveAssetIfDirty(target);
				AssetDatabase.SaveAssets();
				EditorUtility.FocusProjectWindow();
			};
		}
	}
}
