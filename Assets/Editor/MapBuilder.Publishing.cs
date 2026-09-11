using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MapUploader.Rules;
using Plugins.CarX.Modding.Creator.Editor.Publishing;
using Plugins.CarX.Modding.Creator.Runtime;
using Plugins.CarX.Modding.Creator.Runtime.Publishing;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	public static partial class MapBuilder
	{
		/// <summary>
		/// Tagging policy for this game, shared by every vendor. "map_2.0" is what the uploader stamps today, "Map"
		/// is the tag maps published by earlier versions carry and is only used to keep listing them.
		/// </summary>
		public static readonly ModPublisherContext publisherContext = new(
			new[] { "map_2.0" },
			new[] { "Map" },
			ModVisibility.Private);

		private static ModPublisherSession m_session;
		private static string m_titleIconPath;
		private static string m_assetPath;
		private static string m_uploadScene;

		/// <summary>
		/// Registers a new entry on the active vendor and publishes the finished build to it in one step.
		/// A complete build is required: see <see cref="StageBuildForCreate"/> for why that holds for every vendor.
		/// </summary>
		public static async Task CreateNewCommunityItem(
			MapMetaConfig config,
			CancellationToken cancellationToken,
			Action<ModItemKey> callback,
			IProgress<string> progress = null)
		{
			// No modal progress bar here either - see the note in UploadCommunityItem.
			Debug.Log($"Creating a new item on {session.Publisher?.DisplayName ?? "the vendor"}...");

			try
			{
				progress?.Report("Connecting to mod.io…");
				var ready = await session.EnsureInitializedAsync(cancellationToken);
				if (!ready.Success)
				{
					Debug.LogError(ready.Message);
					return;
				}

				// Staged first: without content there is nothing to create, and the vendor must not be touched.
				progress?.Report("Preparing publication files…");
				var contentDirectory = StageBuildForCreate(config);
				if (string.IsNullOrEmpty(contentDirectory))
				{
					return;
				}

				var meta = config.mapMetaConfigValue;
				var previewPath = meta.icon != null
					? Application.dataPath.Substring(0, Application.dataPath.Length - 6) +
					  AssetDatabase.GetAssetPath(meta.icon)
					: string.Empty;

				if (IsBuildTooLarge())
				{
					return;
				}

				var notes = MapManagerConfig.GetPublishData(config);

				var request = new ModCreateRequest(
					meta.mapName,
					BuildSummary(meta),
					previewPath,
					contentDirectory,
					ResolveVersion(meta),
					notes?.changelog,
					publisherContext.DefaultVisibility,
					publisherContext.ContentTags);

				progress?.Report("Sending files / waiting for mod.io…");
				var result = await session.Publisher.CreateItemAsync(request, cancellationToken);

				if (!result.Success)
				{
					Debug.LogError(result.Message);
					return;
				}

				if (!string.IsNullOrWhiteSpace(result.Message))
				{
					Debug.LogWarning(result.Message);
				}

				Debug.Log($"Created community item {result.Value}");
				callback?.Invoke(result.Value);
			}
			catch (OperationCanceledException)
			{
				Debug.LogWarning("Creating the community item was cancelled.");
			}
			catch (Exception exception)
			{
				Debug.LogError($"Could not create the community item: {exception.Message}");
			}
			finally
			{
				EditorUtility.ClearProgressBar();

				if (Directory.Exists(assetBuildPath))
				{
					Directory.Delete(assetBuildPath, true);
				}
			}
		}

		/// <summary>
		/// Stages the finished build of <paramref name="config"/> so it can be published together with the new entry.
		/// Returns an empty path, having explained why, when there is nothing complete to stage.
		/// </summary>
		/// <remarks>
		/// A finished build is a precondition for creating an entry on every vendor. Creating an empty entry first
		/// and filling it in later leaves a half made item behind whenever the second step does not happen, and on
		/// mod.io it is worse than untidy: a mod with no file cannot be read back by the plugin at all.
		/// </remarks>
		private static string StageBuildForCreate(MapMetaConfig config)
		{
			if (config == null)
			{
				Debug.LogError("Assign a Map Meta Config before creating an item.");
				return string.Empty;
			}

			var buildData = MapManagerConfig.GetBuildOrEmpty(config);
			if (!CanStageBuild(buildData)) return string.Empty;

			MapManagerConfig.instance.mapMetaConfigValue = config;
			InitPathUpload(buildData);
			SelectCache();
			BuildDataTransition();

			return assetBuildPath;
		}

		/// <summary>
		/// Pushes a finished build to the active vendor, or copies it into the vendor's local install folder when
		/// <paramref name="localBuild"/> is set and the vendor supports local installs.
		/// </summary>
		public static async Task UploadCommunityItem(
			MapManagerConfig.BuildData buildData,
			ModItem published,
			bool localBuild,
			IProgress<float> progress,
			CancellationToken cancellationToken,
			Action<ModItemKey> callback)
		{
			if (!CanStageBuild(buildData))
			{
				callback?.Invoke(default);
				return;
			}
			InitPathUpload(buildData);
			ApplyVendorLimitsToValidation();
			SelectCache();

			if (localBuild)
			{
				if (string.IsNullOrWhiteSpace(published.LocalInstallDirectory))
				{
					Debug.LogError(
						"Could not work out where the game is installed, so there is nowhere to put a local test " +
						"copy. Check the Steam app id on SteamWorkshopConfig and that the game is installed.");
					return;
				}

				BuildDataTransitionLocal(published.LocalInstallDirectory);
				Debug.Log($"Local test copy written to {published.LocalInstallDirectory}");
				callback?.Invoke(published.Key);
				return;
			}

			// Deliberately no EditorUtility.DisplayProgressBar around this. A modal progress bar held across async
			// work stalls the editor's task pump: everything up to the first real suspension runs, and the
			// continuation never comes back - leaving a bar that cannot be dismissed and an upload that never starts.
			// Progress is reported through the log and the row spinner instead.
			Debug.Log($"Uploading '{published.Title}'...");

			var uploadedKey = default(ModItemKey);

			try
			{
				var ready = await session.EnsureInitializedAsync(cancellationToken);
				if (!ready.Success)
				{
					Debug.LogError(ready.Message);
					return;
				}

				if (IsBuildTooLarge())
				{
					return;
				}

				BuildDataTransition();

				var meta = MapManagerConfig.Value;
				var notes = MapManagerConfig.GetPublishData(MapManagerConfig.instance.mapMetaConfigValue);
				var fields = ModUploadFields.None;

				if (MapManagerConfig.instance.uploadName)
				{
					fields |= ModUploadFields.Title;
				}

				if (MapManagerConfig.instance.uploadDescription)
				{
					fields |= ModUploadFields.Description;
				}

				if (MapManagerConfig.instance.uploadPreview)
				{
					fields |= ModUploadFields.Preview;
				}

				var request = new ModUploadRequest(
					published.Key,
					assetBuildPath,
					meta.mapName,
					BuildSummary(meta),
					meta.mapDescription,
					m_titleIconPath,
					publisherContext.ContentTags,
					// Unknown: an update must not touch the visibility the author set on the mod page.
					ModVisibility.Unknown,
					fields,
					ResolveVersion(meta),
					notes?.changelog);

				var result = await session.Publisher.UploadItemAsync(request, progress, cancellationToken);

				if (!result.Success)
				{
					Debug.LogError(result.Message);
					return;
				}

				Debug.Log(result.Message);
				uploadedKey = published.Key;
			}
			catch (OperationCanceledException)
			{
				Debug.LogWarning("Upload cancelled.");
			}
			catch (Exception exception)
			{
				Debug.LogError($"Could not upload the community item: {exception.Message}");
			}
			finally
			{
				// Fires on failure too, with an invalid key, so the caller can always drop its "busy" state -
				// otherwise a failed upload would leave the window spinning forever.
				callback?.Invoke(uploadedKey);

				EditorUtility.ClearProgressBar();

				if (Directory.Exists(assetBuildPath))
				{
					Directory.Delete(assetBuildPath, true);
				}
			}
		}

		/// <summary>
		/// The author's version label, falling back to 1.0.0 when they left it empty - a file
		/// with no version at all reads as a mistake on the mod page.
		/// Vendors without a version field ignore this; see <see cref="ModVendorLimits.SupportsVersion"/>.
		/// </summary>
		private static string ResolveVersion(MapMetaConfigValue meta)
		{
			return MapMetaRules.ResolveVersion(meta.mapVersion, "1.0.0", Limits.SupportsVersion);
		}

		/// <summary>
		/// Short one line description for vendors that keep a summary separate from the long description.
		/// </summary>
		private static string BuildSummary(MapMetaConfigValue meta)
		{
			return MapMetaRules.BuildSummary(meta.mapName, meta.mapDescription, meta.summary);
		}

	}
}
