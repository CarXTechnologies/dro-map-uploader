using System.IO;
using Plugins.CarX.Modding.Creator.Runtime.Publishing;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	/// <summary>
	/// Creates the per vendor config assets this project needs, pre-filled with the CarX values.
	/// </summary>
	/// <remarks>
	/// The publisher implementations live in a submodule that is meant to be reusable, so they ship without any
	/// game specific credentials and look their settings up from a Resources asset instead. That asset is what this
	/// file plants: the submodule stays generic, the ids stay with the project that owns them.
	/// </remarks>
	public static class ModPublisherConfigSetup
	{
		private const string ConfigDirectory = "Assets/Resources/CarX.Modding";

		/// <summary>Steam app id of CarX Drift Racing Online.</summary>
		private const uint SteamAppId = 635260;

		private const string SteamGameName = "CarX Drift Racing Online";

		private const string ModIoGameName = "CarX Drift Racing Online 2";

		/// <summary>Public mod.io page of the game.</summary>
		private const string ModIoProfileUrl = "https://mod.io/g/carx-dro2";

		[InitializeOnLoadMethod]
		private static void EnsureConfigsExist()
		{
			// Deferred: the asset database is not necessarily ready to take writes during domain load.
			EditorApplication.delayCall += () => CreateMissing(logWhenNothingToDo: false);
		}

		[MenuItem("Tools/CarX Modding/Create publisher configs")]
		private static void CreateMissingFromMenu()
		{
			CreateMissing(logWhenNothingToDo: true);
		}

		private static void CreateMissing(bool logWhenNothingToDo)
		{
			var created = false;

			// Both configs hold a list of games; a fresh asset is seeded with the one entry this project starts from.
			created |= TryCreate<SteamWorkshopConfig>("SteamWorkshopConfig", config =>
			{
				var serialized = new SerializedObject(config);
				var games = serialized.FindProperty("m_games");
				games.arraySize = 1;

				var entry = games.GetArrayElementAtIndex(0);
				entry.FindPropertyRelative("displayName").stringValue = SteamGameName;
				entry.FindPropertyRelative("appId").uintValue = SteamAppId;

				serialized.ApplyModifiedPropertiesWithoutUndo();
			});

			created |= TryCreate<ModIoConfig>("ModIoConfig", config =>
			{
				var serialized = new SerializedObject(config);
				var games = serialized.FindProperty("m_games");
				games.arraySize = 1;

				var entry = games.GetArrayElementAtIndex(0);
				entry.FindPropertyRelative("displayName").stringValue = ModIoGameName;
				entry.FindPropertyRelative("profileUrl").stringValue = ModIoProfileUrl;

				serialized.ApplyModifiedPropertiesWithoutUndo();
			});

			if (created)
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				Debug.Log($"Created the missing mod publisher configs in '{ConfigDirectory}'. " +
				          "Fill in the mod.io game id and api key from the mod.io admin panel (Admin -> API).");
			}
			else if (logWhenNothingToDo)
			{
				Debug.Log($"All mod publisher configs already exist in '{ConfigDirectory}'.");
			}
		}

		private static bool TryCreate<T>(string assetName, System.Action<T> configure) where T : ScriptableObject
		{
			if (Resources.LoadAll<T>(string.Empty).Length > 0)
			{
				return false;
			}

			Directory.CreateDirectory(ConfigDirectory);

			var config = ScriptableObject.CreateInstance<T>();
			configure?.Invoke(config);

			AssetDatabase.CreateAsset(config, $"{ConfigDirectory}/{assetName}.asset");
			return true;
		}
	}
}
