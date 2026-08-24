using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Plugins.CarX.Modding.Creator.Editor;
using Plugins.CarX.Modding.Creator.Runtime;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;
using Unity.EditorCoroutines.Editor;
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

		private static readonly List<GameMarkerData> m_cacheDataList = new();
		private static CacheData m_cacheData;
		public static SteamUGCManager steamUgc;
		private static string m_scenePath;
		private static string m_titleIconPath;
		private static string m_assetPath;
		private static PublishedFileId m_currentFileId;
		private static BuildAssetBundleOptions m_assetBundleOption = BuildAssetBundleOptions.UncompressedAssetBundle;
		private static BuildTarget m_buildTarget = BuildTarget.StandaloneWindows;
		private static FormatBuild m_buildFormat;
		private static string m_uploadScene;
		private static string m_targetScene => MapManagerConfig.instance.targetScene;

		private static readonly IModCollectionProvider m_provider = new EditorCollectionProvider();
		private static ModResults results;

		private static bool PublishCallback(ulong id)
		{
			EditorUtility.ClearProgressBar();
			Directory.Delete(assetBuildPath, true);

			if (id == SteamUGCManager.PUBLISH_ITEM_FAILED_CODE)
			{
				Debug.LogError("Publish failed");
				return false;
			}

			Debug.Log("Export track id: " + id);
			return true;
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

		private static string GetScenePathNoId(string path)
		{
			return path.Substring(0, path.Length - 16) + ".unity";
		}

		private static bool CheckMetaAndError()
		{
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

			if (MapManagerConfig.Value.mapName.Length > 128 && MapManagerConfig.instance.uploadSteamName)
			{
				Debug.LogError("Length name more 128 symbols");
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

			if (new FileInfo(m_titleIconPath).Length / ModMapTestTool.BYTES_TO_MEGABYTES > 1f)
			{
				Debug.LogError("Icon more 1mb");
				return true;
			}

			if (new FileInfo(m_assetPath + AssetDatabase.GetAssetPath(MapManagerConfig.Value.largeIcon)).Length / ModMapTestTool.BYTES_TO_MEGABYTES > 10f)
			{
				Debug.LogError("Large icon more 10mb");
				return true;
			}

			if (MapManagerConfig.Value.mapDescription.Length > 8000 && MapManagerConfig.instance.uploadSteamDescription)
			{
				Debug.LogError($"Map description must be less than 8000 characters({MapManagerConfig.Value.mapDescription.Length})");
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

		public static void InitSteamUgc()
		{
			try
			{
				if (steamUgc == null)
				{
					SteamClient.Init(SteamUGCManager.APP_ID, false);
					steamUgc = new SteamUGCManager();
					EditorApplication.update += steamUgc.Update;
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning(e);
			}
		}

		private static bool IsValidate(Scene scene)
		{
			var isError = false;

			ModMapTestTool.errorCallback = (_, error) =>
			{
				Debug.LogError(error);
				EditorUtility.ClearProgressBar();
				if (m_currentFileId != 0)
				{
					SteamUGC.DeleteFileAsync(m_currentFileId);
					m_currentFileId = 0;
				}

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

			ModMapTestTool.Target = (ValidItemData)ModMapTestTool.Steam.Clone();

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

		private static void RenameCacheScene(PublishedFileId publishResult)
		{
			var sceneName = GetSceneNameFromPathNoId(m_targetScene);
			var scenePathNew = path + "/" + sceneName + publishResult.Value + ".unity";
			AssetDatabase.RenameAsset(m_scenePath, sceneName + publishResult.Value);
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
						Id = m_currentFileId.Value.ToString(),
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
			PublishedFileId published,
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
				m_currentFileId = published;

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
								RenameCacheScene(published);
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

		public static void CreateNewCommunityFile(Action<PublishResult> callback)
		{
			EditorUtility.DisplayProgressBar("Creating Community File...", string.Empty, 1f);
			EditorCoroutineUtility.StartCoroutine(steamUgc.CreatePublisherItem(result =>
			{
				callback?.Invoke(result);
				EditorUtility.ClearProgressBar();
			}), steamUgc);
		}

		public static void UploadSteamCommunityItem(
			MapManagerConfig.BuildData buildData,
			Item published,
			bool localBuild,
			Action<PublishedFileId> callback)
		{
			InitPathUpload(buildData);
			ModMapTestTool.Target = (ValidItemData)ModMapTestTool.Steam.Clone();

			SelectCache();

			if (localBuild)
			{
				BuildDataTransitionLocal(published.Directory);
				Debug.Log($"Move build to folder successful ({published.Directory})");
			}
			else
			{
				EditorUtility.DisplayProgressBar("Uploading Community File...", string.Empty, 1f);
				ClearDirectory(assetBuildPath);
				BuildDataTransition();

				if (IsSizeValid())
				{
					EditorUtility.ClearProgressBar();
					return;
				}

				steamUgc.SetItemData(MapManagerConfig.Value.mapName, m_titleIconPath, MapManagerConfig.Value.mapDescription);
				EditorCoroutineUtility.StartCoroutine(steamUgc.UploadItemCoroutine(assetBuildPath, published.Id,
					publish =>
					{
						if (!PublishCallback(publish)) return false;

						callback?.Invoke(publish);
						return true;
					}), steamUgc);
			}
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
			ClearDirectory(directory, false);
			CopyTemporary(GetTemporary(TempData.Map), directory);
			CopyTemporary(GetTemporary(TempData.Meta), directory);
			ClearManifest(directory);
		}

		public static void BuildDataTransitionToDirectory(MapManagerConfig.BuildData buildData, string directory)
		{
			InitPathUpload(buildData);
			ModMapTestTool.Target = (ValidItemData)ModMapTestTool.Steam.Clone();
			SelectCache();

			CopyTemporary(GetTemporary(TempData.Map), directory);
			CopyTemporary(GetTemporary(TempData.Meta), directory);
			ClearManifest(directory);
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

		private static bool IsSizeValid()
		{
			var sceneName = GetSceneNameFromPathNoId(m_uploadScene);
			var notMapSize = ModMapTestTool.IsNotCorrectMapFileSize(sceneName + ".bundle", assetBuildPath + "/" + sceneName + ".bundle");
			var notMetaSize = ModMapTestTool.IsNotCorrectMetaFileSize(assetBuildPath + "/" + nameof(TempData.Meta).ToLower());

			if (!notMapSize && !notMetaSize)
			{
				return false;
			}

			Directory.Delete(assetBuildPath, true);
			return true;
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