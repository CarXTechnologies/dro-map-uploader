using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Plugins.CarX.Modding.Creator.Editor;
using Plugins.CarX.Modding.Creator.Editor.Publishing;
using Plugins.CarX.Modding.Creator.Runtime;
using Plugins.CarX.Modding.Creator.Runtime.Publishing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
	public class MapBuilder : MonoBehaviour
	{
		private static readonly string assetDir = Application.temporaryCachePath + "/";
		private static readonly string assetBuildPath = assetDir + "Standalone";
		private static readonly string assetBuildPathTemporaryOrigin = assetDir + "StandaloneTemporary";
		private static string assetBuildPathTemporary = assetBuildPathTemporaryOrigin;
		private const string path = "Assets";

		/// <summary>
		/// Tagging policy for this game, shared by every vendor. "map_2.0" is what the uploader stamps today, "Map"
		/// is the tag maps published by earlier versions carry and is only used to keep listing them.
		/// </summary>
		public static readonly ModPublisherContext publisherContext = new(
			new[] { "map_2.0" },
			new[] { "Map" },
			ModVisibility.Private);

		private static readonly List<GameMarkerData> m_cacheDataList = new();
		private static CacheData m_cacheData;
		private static ModPublisherSession m_session;
		private static string m_scenePath;
		private static string m_titleIconPath;
		private static string m_assetPath;
		private static ModItemKey m_currentItemKey;
		private static BuildAssetBundleOptions m_assetBundleOption = BuildAssetBundleOptions.UncompressedAssetBundle;
		private static BuildTarget m_buildTarget = BuildTarget.StandaloneWindows;
		private static FormatBuild m_buildFormat;
		private static string m_uploadScene;
		private static string m_targetScene => MapManagerConfig.instance.targetScene;

		private static readonly IModCollectionProvider m_provider = new EditorCollectionProvider();
		private static ModResults results;

		/// <summary>The publisher the uploader currently talks to. Created on first use and reused afterwards.</summary>
		public static ModPublisherSession session => m_session ??= new ModPublisherSession(publisherContext);

		/// <summary>
		/// Limits of the active vendor, or the conservative defaults baked into the component rules when no vendor
		/// could be brought up - validation still has to produce sensible numbers in that state.
		/// </summary>
		public static ModVendorLimits Limits => session.Limits ?? new ModVendorLimits(
			ModMapTestTool.ComponentRules.maxSizeInMb,
			ModMapTestTool.ComponentRules.maxSizeInMbMeta,
			1f, 128, 8000, 8000,
			supportsLocalInstall: false, requiresSummary: false, requiresPreviewOnCreate: false,
			supportsVersion: false);

		/// <summary>Rebuilds the validation target from the component rules plus the active vendor's size caps.</summary>
		private static void ApplyVendorLimitsToValidation()
		{
			var limits = Limits;
			ModMapTestTool.Target = ModMapTestTool.ComponentRules
				.CloneWithLimits(limits.MaxPayloadSizeInMb, limits.MaxMetaSizeInMb);
		}

		private static bool IsCurrentSceneCheck()
		{
			var currentScene = SceneManager.GetActiveScene();

			return m_targetScene != currentScene.path && !EditorUtility.DisplayDialog($"Build scene : {GetSceneNameFromPathNoId(m_targetScene)}", $"Close and save the current scene : {currentScene.name}", "Yes", "Cancel");
		}

		public static string GetSceneNameFromPathNoId(string path)
		{
			var pos = path.LastIndexOf('/');
			return pos == -1 ? path : path.Substring(pos + 1, path.Length - pos - 7);
		}

		/// <summary>
		/// Id the current build stamps into its output.
		/// </summary>
		/// <remarks>
		/// Building is a local operation on a map config and must not require a vendor entry to exist - on mod.io an
		/// entry cannot even be created without a payload to attach, so demanding one here would be a deadlock. Until
		/// there is an entry the map config's own id stands in, and the meta is rebuilt once the entry exists.
		/// </remarks>
		private static string CurrentBuildId => m_currentItemKey.IsValid
			? m_currentItemKey.id
			: MapManagerConfig.instance.mapMetaConfigValue.id;

		private static string GetScenePathNoId(string path)
		{
			// The suffix is the id plus ".unity"; ids are not a fixed width - Steam hands out ten digit numbers,
			// mod.io five digit ones, and a map config id is a 32 character guid.
			return path.Substring(0, path.Length - (CurrentBuildId.Length + ".unity".Length)) + ".unity";
		}

		private static bool CheckMetaAndError()
		{
			var limits = Limits;

			if (!GetSceneNameFromPathNoId(m_targetScene).All(char.IsLetter))
			{
				Debug.LogError("Target scene only letters");
				return true;
			}

			if (string.IsNullOrWhiteSpace(MapManagerConfig.Value.mapName))
			{
				Debug.LogError("Please name your track");
				return true;
			}

			if (!MapManagerConfig.Value.mapName.All(char.IsLetter))
			{
				Debug.LogError("Track name only letters");
				return true;
			}

			if (MapManagerConfig.Value.mapName.Length > limits.MaxTitleLength && MapManagerConfig.instance.uploadName)
			{
				Debug.LogError($"Length name more {limits.MaxTitleLength} symbols");
				return true;
			}

			if (MapManagerConfig.Value.icon == null)
			{
				Debug.LogError($"Please apply icon config({MapManagerConfig.instance.mapMetaConfigValue.name}) field");
				return true;
			}

			if (!MapManagerConfig.Value.largeIcon.isReadable || !MapManagerConfig.Value.icon.isReadable)
			{
				Debug.LogError("Icon no valid format");
				return true;
			}

			if (new FileInfo(m_titleIconPath).Length / ModMapTestTool.BYTES_TO_MEGABYTES > limits.MaxPreviewSizeInMb)
			{
				Debug.LogError($"Icon more {limits.MaxPreviewSizeInMb}mb");
				return true;
			}

			if (new FileInfo(m_assetPath + AssetDatabase.GetAssetPath(MapManagerConfig.Value.largeIcon)).Length / ModMapTestTool.BYTES_TO_MEGABYTES > 10f)
			{
				Debug.LogError("Large icon more 10mb");
				return true;
			}

			if (MapManagerConfig.Value.mapDescription.Length > limits.MaxDescriptionLength &&
			    MapManagerConfig.instance.uploadDescription)
			{
				Debug.LogError($"Map description must be less than {limits.MaxDescriptionLength} characters({MapManagerConfig.Value.mapDescription.Length})");
				return true;
			}

			return false;
		}

		private static void ClearDirectory(string path, bool recursive = true)
		{
			if (recursive)
			{
				if (Directory.Exists(path))
				{
					Directory.Delete(path, recursive);
				}

				Directory.CreateDirectory(path);
			}
			else
			{
				foreach (var file in Directory.GetFiles(path))
				{
					File.Delete(file);
				}
			}
		}

		private static void CopyTemporary(string source, string dest)
		{
			DirectoryInfo sourceDirectory = new DirectoryInfo(source);

			if (!sourceDirectory.Exists)
			{
				throw new DirectoryNotFoundException($"Source directory not found: {source}");
			}

			Directory.CreateDirectory(dest);

			foreach (FileInfo file in sourceDirectory.GetFiles())
			{
				string targetFilePath = Path.Combine(dest, file.Name);
				file.CopyTo(targetFilePath, true);
			}

			foreach (DirectoryInfo subdir in sourceDirectory.GetDirectories())
			{
				string newTargetDir = Path.Combine(dest, subdir.Name);
				CopyTemporary(subdir.FullName, newTargetDir);
			}
		}

		private static void ClearCacheScene()
		{
			foreach (var file in Directory.GetFiles(path, "*.unity"))
			{
				File.Delete(file);
			}
		}

		private static bool IsValidate(Scene scene)
		{
			var isError = false;

			ModMapTestTool.errorCallback = (_, error) =>
			{
				Debug.LogError(error);
				EditorUtility.ClearProgressBar();
				isError = true;
			};

			ModMapTestTool.Play(MapManagerConfig.Value.mapName)?.WithList(ModMapTestTool.Target.data).ValidComponents();
			ModMapTestTool.InitTestsEditor(scene);
			//no dro2
			//ModMapTestTool.RunTest(m_targetScene);

			return isError;
		}

		private static void InitPath()
		{
			var targetScene = GetSceneNameFromPathNoId(m_targetScene);
			m_scenePath = path + "/" + targetScene + ".unity";
			m_assetPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
			m_titleIconPath = m_assetPath + AssetDatabase.GetAssetPath(MapManagerConfig.Value.icon);
		}

		private static void InitPathUpload(MapManagerConfig.BuildData scenePath)
		{
			m_uploadScene = GetSceneNameFromPathNoId(scenePath.targetScene);
			m_scenePath = path + "/" + m_uploadScene + ".unity";
			m_assetPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
			m_titleIconPath = m_assetPath + AssetDatabase.GetAssetPath(MapManagerConfig.Value.icon);
		}

		private static bool? ValidComponent(Component component)
		{
			var compType = component.GetType();
			if (!ModMapTestTool.ValidType(component, ModMapTestTool.Target.data))
			{
				if (ModMapTestTool.ValidType(compType, MapSkipComponentConfig.instance.valid))
				{
					return true;
				}

				ModMapTestTool.TryErrorMessage(compType.Name, $"No valid component : {compType.Name}");
				return false;
			}

			return null;
		}

		private static bool? ProcessComponent(Component component)
		{
			var compType = component.GetType();

			if (compType.Name == nameof(GameMarkerData))
			{
				var comp = component.GetComponent<GameMarkerData>();
				comp.markerData.Update();
				m_cacheDataList.Add(comp);
				if (comp.markerData.GetHead() == "road")
				{
					var road = comp.gameObject;
					road.isStatic = true;
					GameObjectUtility.SetStaticEditorFlags(road,
						StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic |
						StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic |
						StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.OffMeshLinkGeneration);
				}
			}

			if (compType.Name == nameof(LODGroup))
			{
				var groupLods = (component as LODGroup)?.GetLODs();

				for (var i = 0; i < groupLods.Length; i++)
				{
					groupLods[i].renderers = component.transform.FindAllComponent<Renderer>(groupLods[i].renderers);
				}

				component.GetComponent<LODGroup>().SetLODs(groupLods);
			}

			if (compType.Name == nameof(ReflectionProbe))
			{
				m_cacheData.reflectionProbe = component.GetComponent<ReflectionProbe>();
			}

			return null;
		}

		[Obsolete("Obsolete")]
		private static bool ValidateSceneAndMirror()
		{
			EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
			EditorSceneManager.OpenScene(m_targetScene);
			var scene = SceneManager.GetActiveScene();
			var sceneObjects = scene.GetRootGameObjects();
			var root = new GameObject("root");

			for (var i = 0; i < sceneObjects.Length; i++)
			{
				sceneObjects[i].transform.SetParent(root.transform);
			}

			var mapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

			m_cacheDataList.Clear();
			m_cacheData = new GameObject("CacheData", typeof(CacheData)).GetComponent<CacheData>();

			ApplyVendorLimitsToValidation();

			if (IsValidate(scene))
			{
				EditorSceneManager.OpenScene(m_targetScene);
				try
				{
					SceneManager.UnloadSceneAsync(mapScene);
				}
				catch
				{
					// ignored
				}

				DestroyImmediate(root);
				return true;
			}

			IModResultCollector collector = null;

			switch (m_buildFormat)
			{
				case FormatBuild.dro1:
					collector = new SceneAssetBundleCollector(root.transform, ValidComponent, ProcessComponent, "Garbage");
					break;
				case FormatBuild.dro2:
					collector = new SceneFormatCollector(root.transform, Path.GetFileNameWithoutExtension(m_scenePath), "Garbage");
					break;
			}

			results = collector.CollectModResults(m_provider, ModdingVersion.GetFullVersionFormat());

			for (var i = 0; i < sceneObjects.Length; i++)
			{
				sceneObjects[i].transform.SetParent(null);
			}

			m_cacheData.gameMarkers = new List<GameMarkerData>(m_cacheDataList.ToArray());

			DestroyImmediate(root);

			EditorSceneManager.SaveScene(mapScene, m_scenePath);
			SceneManager.UnloadScene(mapScene);

			return !results.success;
		}

		private static string GetTemporary(TempData name)
		{
			var pathDir = Path.Combine(assetBuildPathTemporary, name + "Temp");
			if (!Directory.Exists(pathDir))
			{
				Directory.CreateDirectory(pathDir);
			}

			return pathDir;
		}

		private static void RenameCacheScene()
		{
			var sceneName = GetSceneNameFromPathNoId(m_targetScene);
			var scenePathNew = path + "/" + sceneName + CurrentBuildId + ".unity";
			AssetDatabase.RenameAsset(m_scenePath, sceneName + CurrentBuildId);
			m_scenePath = scenePathNew;
		}

		private static string GetCacheName()
		{
			return MapManagerConfig.instance.mapMetaConfigValue.id;
		}

		private static void CreateMapBundle(FormatBuild moddingFormat)
		{
			switch (moddingFormat)
			{
				case FormatBuild.dro1:
					var sceneName = GetSceneNameFromPathNoId(m_targetScene);
					var bundleBuilds = CreateBundleArrayDataForOneElement(sceneName + ".bundle", GetScenePathNoId(m_scenePath));
					BuildPipeline.BuildAssetBundles(GetTemporary(TempData.Map), bundleBuilds, m_assetBundleOption, m_buildTarget);
					return;
				case FormatBuild.dro2:
					results.UploadInCatalog(GetTemporary(TempData.Map));
					return;
			}
		}

		private static void CreateMetaBundle(FormatBuild moddingFormat)
		{
			MapMetaConfigValue metaValue = MapManagerConfig.Value;
			metaValue.compress = MapManagerConfig.Build.compress;
			metaValue.platform = MapManagerConfig.Build.platform;
			MapManagerConfig.instance.mapMetaConfigValue.mapMetaConfigValue = metaValue;

			var pathToResources = "Assets/Resources/" + MapManagerConfig.instance.name + ".asset";

			switch (moddingFormat)
			{
				case FormatBuild.dro1:
					var bundleBuilds = CreateBundleArrayDataForOneElement(nameof(TempData.Meta).ToLower() + ".bundle", pathToResources);
					BuildPipeline.BuildAssetBundles(GetTemporary(TempData.Meta), bundleBuilds, m_assetBundleOption, m_buildTarget);
					return;
				case FormatBuild.dro2:
					results ??= new ModResults(m_provider);
					var modHierarchy = new ModMeta
					{
						Id = CurrentBuildId,
						name = metaValue.mapName,
						description = metaValue.mapDescription,
						madeIn = $"Mod Map Uploader {ModdingVersion.GetFullVersion()}",
						Version = ModdingVersion.GetFullVersionFormat(),
						authors = metaValue.authors,
						url = metaValue.url
					};

					var decompressedIcon = metaValue.icon != null
						? UnityGoObjExporter.EnsureTextureIsReadableAndUncompressed(metaValue.icon) : null;
					var decompressedLargeIcon = metaValue.largeIcon != null
						? UnityGoObjExporter.EnsureTextureIsReadableAndUncompressed(metaValue.largeIcon) : null;

					if (results.TryGetProvider(decompressedIcon, out var iconProvider))
					{
						modHierarchy.icon = iconProvider.GetFilePath(decompressedIcon);
					}

					if (results.TryGetProvider(decompressedLargeIcon, out var largeIconProvider))
					{
						modHierarchy.largeIcon = largeIconProvider.GetFilePath(decompressedLargeIcon);
					}

					if (decompressedIcon != null)
					{
						results.Add(decompressedIcon);
					}
					if (decompressedLargeIcon != null)
					{
						results.Add(decompressedLargeIcon);
					}

					modHierarchy.minimap = CollectMinimapMeta();

					results.Add(modHierarchy);
					results.UploadInCatalog(GetTemporary(TempData.Meta));
					return;
			}
		}

		private static ModMinimapMeta CollectMinimapMeta()
		{
			var minimap = UnityEngine.Object.FindFirstObjectByType<Minimap>();
			if (minimap == null || minimap.Textures == null)
			{
				return null;
			}

			var textures = minimap.Textures;
			var textureNames = new string[textures.Length];

			for (var i = 0; i < textures.Length; i++)
			{
				textureNames[i] = AddMinimapTexture(textures[i].mainTexture);
			}

			return new ModMinimapMeta
			{
				textures = textureNames,
				boundsCenterX = minimap.BoundsCenter.x,
				boundsCenterY = minimap.BoundsCenter.y,
				boundsSizeX = minimap.BoundsSize.x,
				boundsSizeY = minimap.BoundsSize.y,
			};
		}

		private static string AddMinimapTexture(Texture texture)
		{
			if (texture == null)
			{
				return string.Empty;
			}

			var decompressedTexture = UnityGoObjExporter.EnsureTextureIsReadableAndUncompressed(texture as Texture2D);
			if (decompressedTexture == null || !results.TryGetProvider(decompressedTexture, out var provider))
			{
				return string.Empty;
			}

			var filePath = provider.GetFilePath(decompressedTexture);
			results.Add(decompressedTexture);
			return filePath;
		}

		private static void SelectCache()
		{
			assetBuildPathTemporary = assetBuildPathTemporaryOrigin + GetCacheName();
		}

		[Obsolete("Obsolete")]
		public static async void BuildCustom(
			TempData target,
			TempData success,
			ModItemKey itemKey,
			FormatBuild formatBuild,
			CompressBuild compressBuild,
			PlatformBuild platformBuild,
			Action<string, TempData> callback)
		{
			try
			{
				switch (compressBuild)
				{
					case CompressBuild.NoCompress:
						m_assetBundleOption = BuildAssetBundleOptions.UncompressedAssetBundle;
						break;
					case CompressBuild.Compress:
						m_assetBundleOption = BuildAssetBundleOptions.None;
						break;
				}

				switch (platformBuild)
				{
					case PlatformBuild.StandaloneWindows:
						m_buildTarget = BuildTarget.StandaloneWindows;
						break;
				}

				m_buildFormat = formatBuild;
				m_currentItemKey = itemKey;

				SelectCache();

				if (target.HasFlag(TempData.Meta))
				{
					ClearDirectory(GetTemporary(TempData.Meta));
				}

				if (target.HasFlag(TempData.Map))
				{
					ClearDirectory(GetTemporary(TempData.Map));
				}

				InitPath();

				if (!CheckMetaAndError())
				{
					if (target.HasFlag(TempData.Map))
					{
						if (!IsCurrentSceneCheck())
						{
							if (!ValidateSceneAndMirror())
							{
								RenameCacheScene();
								CreateMapBundle(m_buildFormat);
								success |= TempData.Map;
							}
							else
							{
								success = (TempData)((int)success & ~(int)TempData.Map);
							}
						}
						else
						{
							success = (TempData)((int)success & ~(int)TempData.Map);
						}
					}

					if (target.HasFlag(TempData.Meta))
					{
						CreateMetaBundle(m_buildFormat);
						ClearCacheScene();
						success |= TempData.Meta;
					}
				}
				else
				{
					success = (TempData)((int)success & ~(int)TempData.Meta);
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
				throw;
			}
			finally
			{
				while (BuildPipeline.isBuildingPlayer) await Task.Delay(100);
				callback?.Invoke(assetBuildPathTemporary, success);

				MapManagerConfig.SaveForce();
			}
		}

		/// <summary>
		/// Registers a new entry on the active vendor and publishes the finished build to it in one step.
		/// A complete build is required: see <see cref="StageBuildForCreate"/> for why that holds for every vendor.
		/// </summary>
		public static async void CreateNewCommunityItem(MapMetaConfig config, Action<ModItemKey> callback)
		{
			// No modal progress bar here either - see the note in UploadCommunityItem.
			Debug.Log($"Creating a new item on {session.Publisher?.DisplayName ?? "the vendor"}...");

			try
			{
				var ready = await session.EnsureInitializedAsync(CancellationToken.None);
				if (!ready.Success)
				{
					Debug.LogError(ready.Message);
					return;
				}

				// Staged first: without content there is nothing to create, and the vendor must not be touched.
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
					ResolveVersion(notes),
					notes?.changelog,
					publisherContext.DefaultVisibility,
					publisherContext.ContentTags);

				var result = await session.Publisher.CreateItemAsync(request, CancellationToken.None);

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
			var built = (TempData)buildData.buildSuccess;
			var missing = (TempData.Map | TempData.Meta) & ~built;

			if (missing != 0)
			{
				Debug.LogError(
					$"'{config.name}' is not fully built ({missing} missing), so there is nothing to publish. " +
					"Build Map and Meta first, then create the item.");
				return string.Empty;
			}

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
		public static async void UploadCommunityItem(
			MapManagerConfig.BuildData buildData,
			ModItem published,
			bool localBuild,
			Action<ModItemKey> callback)
		{
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
				var ready = await session.EnsureInitializedAsync(CancellationToken.None);
				if (!ready.Success)
				{
					Debug.LogError(ready.Message);
					return;
				}

				if (IsBuildTooLarge())
				{
					return;
				}

				ClearDirectory(assetBuildPath);
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
					ResolveVersion(notes),
					notes?.changelog);

				var result = await session.Publisher.UploadItemAsync(request, null, CancellationToken.None);

				if (!result.Success)
				{
					Debug.LogError(result.Message);
					return;
				}

				Debug.Log(result.Message);
				uploadedKey = published.Key;
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
		/// The author's version label, falling back to the uploader's own version when they left it empty - a file
		/// with no version at all reads as a mistake on the mod page.
		/// Vendors without a version field ignore this; see <see cref="ModVendorLimits.SupportsVersion"/>.
		/// </summary>
		private static string ResolveVersion(MapManagerConfig.PublishData notes)
		{
			if (!Limits.SupportsVersion)
			{
				return string.Empty;
			}

			return string.IsNullOrWhiteSpace(notes?.version) ? ModdingVersion.GetFullVersion() : notes.version.Trim();
		}

		/// <summary>
		/// Short one line description for vendors that keep a summary separate from the long description.
		/// The map config has no dedicated summary field, so the first line of the description stands in for it.
		/// </summary>
		private static string BuildSummary(MapMetaConfigValue meta)
		{
			var description = meta.mapDescription;

			if (string.IsNullOrWhiteSpace(description))
			{
				return string.IsNullOrWhiteSpace(meta.mapName) ? string.Empty : $"{meta.mapName} track.";
			}

			var firstBreak = description.IndexOfAny(new[] { '\r', '\n' });
			return (firstBreak == -1 ? description : description.Substring(0, firstBreak)).Trim();
		}

		private static void BuildDataTransition()
		{
			ClearDirectory(assetBuildPath);
			CopyTemporary(GetTemporary(TempData.Map), assetBuildPath);
			CopyTemporary(GetTemporary(TempData.Meta), assetBuildPath);
			ClearManifest(assetBuildPath);
		}

		private static void BuildDataTransitionLocal(string directory)
		{
			// The game's mods folder may not exist yet, and neither may the mod's own folder inside it - this is the
			// first thing that ever writes there, unlike the old workshop cache which Steam had already created.
			Directory.CreateDirectory(directory);

			ClearDirectory(directory, false);
			CopyTemporary(GetTemporary(TempData.Map), directory);
			CopyTemporary(GetTemporary(TempData.Meta), directory);
			ClearManifest(directory);
		}

		public static bool BuildDataTransitionToDirectory(MapManagerConfig.BuildData buildData, string directory)
		{
			var missing = (TempData.Map | TempData.Meta) & ~(TempData)buildData.buildSuccess;

			if (buildData.config == null || missing != 0)
			{
				Debug.LogError(
					$"'{buildData.config?.name ?? "<no config>"}' is not fully built ({missing} missing), so there is " +
					"nothing to export. Build Map and Meta first.");
				return false;
			}

			InitPathUpload(buildData);
			ApplyVendorLimitsToValidation();
			SelectCache();

			CopyTemporary(GetTemporary(TempData.Map), directory);
			CopyTemporary(GetTemporary(TempData.Meta), directory);
			ClearManifest(directory);
			return true;
		}

		private static void ClearManifest(string directory)
		{
			var dir = new DirectoryInfo(directory);

			foreach (var file in dir.EnumerateFiles("*.manifest"))
			{
				file.Delete();
			}

			foreach (var file in dir.EnumerateFiles("*Temp"))
			{
				file.Delete();
			}
		}

		/// <summary>
		/// Whether the staged build exceeds the size the active vendor accepts.
		/// The two staging folders are measured rather than individual files: dro1 writes asset bundles while dro2
		/// writes a whole catalog of loose files, and the folders are the one thing both formats have in common.
		/// </summary>
		private static bool IsBuildTooLarge()
		{
			var limits = Limits;

			var mapSizeInMb = GetDirectorySizeInMb(GetTemporary(TempData.Map));
			var metaSizeInMb = GetDirectorySizeInMb(GetTemporary(TempData.Meta));

			var tooLarge = false;

			if (mapSizeInMb > limits.MaxPayloadSizeInMb)
			{
				ModMapTestTool.TryErrorMessage(m_uploadScene,
					$"Map size is {mapSizeInMb:F2}/{limits.MaxPayloadSizeInMb} mb");
				tooLarge = true;
			}

			if (metaSizeInMb > limits.MaxMetaSizeInMb)
			{
				ModMapTestTool.TryErrorMessage(m_uploadScene,
					$"Meta size is {metaSizeInMb:F2}/{limits.MaxMetaSizeInMb} mb");
				tooLarge = true;
			}

			return tooLarge;
		}

		private static float GetDirectorySizeInMb(string directory)
		{
			if (!Directory.Exists(directory))
			{
				return 0f;
			}

			var totalBytes = 0L;

			foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
			{
				// Manifests are stripped before upload, so counting them would fail builds that actually fit.
				if (file.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				totalBytes += new FileInfo(file).Length;
			}

			return totalBytes / ModMapTestTool.BYTES_TO_MEGABYTES;
		}

		private static AssetBundleBuild[] CreateBundleArrayDataForOneElement(string bundleName, string path)
		{
			var bundleBuilds = new AssetBundleBuild[1];
			bundleBuilds[0].assetBundleName = bundleName;
			bundleBuilds[0].assetNames = new[] { path};
			var asset = AssetImporter.GetAtPath(bundleBuilds[0].assetNames[0]);
			asset.assetBundleName = bundleBuilds[0].assetBundleName;
			AssetDatabase.RemoveUnusedAssetBundleNames();

			return bundleBuilds;
		}
	}
}
