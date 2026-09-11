using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Editor.Validation;
using MapUploader.Rules;
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
	public static partial class MapBuilder
	{
		public static ValidItemData LastSceneValidation { get; private set; }
        public static bool IsBuilding { get; private set; }


		private static ModItemKey m_currentItemKey;
		private static FormatBuild m_buildFormat;
		private static string m_targetScene => MapManagerConfig.instance.targetScene;

		private static IModCollectionProvider m_provider = new EditorCollectionProvider();
		private static ModResults m_results;

		private static MapValidationReport m_report;

		/// <summary>The publisher the uploader currently talks to. Created on first use and reused afterwards.</summary>
		public static ModPublisherSession session => m_session ??= new ModPublisherSession(publisherContext);

		/// <summary>
		/// Limits of the active vendor, or the conservative defaults baked into the component rules when no vendor
		/// could be brought up - validation still has to produce sensible numbers in that state.
		/// </summary>
		public static ModVendorLimits Limits => session.Limits ?? new ModVendorLimits(
			MapSceneRules.ComponentRules.maxSizeInMb,
			MapSceneRules.ComponentRules.maxSizeInMbMeta,
			1f, 128, 8000, 8000,
			supportsLocalInstall: false, requiresSummary: false, requiresPreviewOnCreate: false,
			supportsVersion: false);

		/// <summary>Rebuilds the validation target from the component rules plus the active vendor's size caps.</summary>
		private static void ApplyVendorLimitsToValidation()
		{
			var limits = Limits;
			LastSceneValidation = MapSceneRules.ComponentRules
				.CloneWithLimits(limits.MaxPayloadSizeInMb, limits.MaxMetaSizeInMb);
		}

		public static string GetSceneNameFromPathNoId(string path)
		{
			return MapMetaRules.GetSceneNameFromPath(path);
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

		/// <summary>
		/// Whether <paramref name="format"/> may be built by this editor, with the reason when it may not.
		/// </summary>
		public static bool IsFormatBlocked(FormatBuild format, out string reason)
		{
			reason = format == FormatBuild.Wavefront || format == FormatBuild.Binary
				? string.Empty : "Unsupported map format. Select Wavefront or Binary and rebuild Map and Meta.";
			return reason.Length != 0;
		}

		private static void ValidateMeta()
		{
			MapMetaValidator.Validate(m_report, MapManagerConfig.Value, m_targetScene, Limits);

			if (m_report.HasErrors)
			{
				m_report.Info(MapMetaValidator.CategoryMeta,
					"The scene itself was not validated - fix the configuration first, then build again.");
			}
		}

		private static bool HasSceneErrors(GameObject[] roots)
		{
			SceneValidator.Validate(
				m_report,
				roots,
				m_buildFormat,
				LastSceneValidation,
				MapSkipComponentConfig.instance.valid);

			return m_report.HasErrors;
		}

		private static void PresentReport(Action revalidate = null, bool showWhenClean = false)
		{
			if (m_report == null)
			{
				return;
			}

			m_report.WriteSummaryToConsole();

			if (m_report.IsEmpty && !showWhenClean)
			{
				MapValidationWindow.CloseIfOpen();
				return;
			}

			MapValidationWindow.Show(m_report, revalidate);
		}

		private static void InitPath()
		{
			m_assetPath = Path.GetDirectoryName(Application.dataPath);
			m_titleIconPath = Path.Combine(m_assetPath, AssetDatabase.GetAssetPath(MapManagerConfig.Value.icon));
		}

		private static void InitPathUpload(MapManagerConfig.BuildData buildData)
		{
			if (IsFormatBlocked(buildData.format, out var reason)) throw new InvalidOperationException(reason);
			MapManagerConfig.instance.mapMetaConfigValue = buildData.config;
			m_buildFormat = buildData.format;
			m_uploadScene = GetSceneNameFromPathNoId(buildData.targetScene);
			InitPath();
		}

		private static bool CollectScene()
		{
			var scene = SceneManager.GetActiveScene();
			var roots = scene.GetRootGameObjects();
			if (HasSceneErrors(roots)) return false;

			var collector = new SceneFormatCollector(roots.Select(root => root.transform), scene.name, "Garbage");
			m_results = collector.CollectModResults(m_provider, ModdingVersion.GetFullVersionFormat());
			return m_results.success;
		}

		private static async Task ExportMap(Action<float> progress, CancellationToken token)
		{
			await m_results.UploadInCatalogAsync(GetTemporary(TempData.Map), progress, token);
		}

		private static async Task ExportMeta(Action<float> progress, CancellationToken token)
		{
			var metaValue = MapManagerConfig.Value;
			m_results = new ModResults(m_provider);
			var modHierarchy = new ModMeta
			{
				Id = CurrentBuildId,
				name = metaValue.mapName,
				description = metaValue.mapDescription,
				madeIn = $"Mod Map Uploader {ModdingVersion.GetFullVersion()}",
				Version = ModdingVersion.GetFullVersionFormat(),
				contentVersion = string.IsNullOrWhiteSpace(metaValue.mapVersion) ? "1.0.0" : metaValue.mapVersion.Trim(),
				authors = metaValue.authors,
				url = metaValue.url
			};

			var decompressedIcon = metaValue.icon != null
				? UnityGoObjExporter.EnsureTextureIsReadableAndUncompressed(metaValue.icon) : null;
			var decompressedLargeIcon = metaValue.largeIcon != null
				? UnityGoObjExporter.EnsureTextureIsReadableAndUncompressed(metaValue.largeIcon) : null;

			if (m_results.TryGetProvider(decompressedIcon, out var iconProvider))
			{
				modHierarchy.icon = iconProvider.GetFilePath(decompressedIcon);
			}

			if (m_results.TryGetProvider(decompressedLargeIcon, out var largeIconProvider))
			{
				modHierarchy.largeIcon = largeIconProvider.GetFilePath(decompressedLargeIcon);
			}

			if (decompressedIcon != null)
			{
				m_results.Add(decompressedIcon);
			}
			if (decompressedLargeIcon != null)
			{
				m_results.Add(decompressedLargeIcon);
			}

			modHierarchy.minimap = CollectMinimapMeta();

			m_results.Add(modHierarchy);
			await m_results.UploadInCatalogAsync(GetTemporary(TempData.Meta), progress, token);
		}

		private static ModMinimapMeta CollectMinimapMeta()
		{
			var minimap = SceneManager.GetActiveScene().GetRootGameObjects()
				.SelectMany(root => root.GetComponentsInChildren<Minimap>())
				.FirstOrDefault();
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
			if (decompressedTexture == null || !m_results.TryGetProvider(decompressedTexture, out var provider))
			{
				return string.Empty;
			}

			var filePath = provider.GetFilePath(decompressedTexture);
			m_results.Add(decompressedTexture);
			return filePath;
		}

		public static async Task BuildCustom(
			TempData target,
			TempData success,
			ModItemKey itemKey,
			FormatBuild formatBuild,
			IProgress<string> progress,
			CancellationToken cancellationToken,
			Action<string, TempData> callback, Action<float> fraction = null)
		{
            if (IsBuilding) throw new InvalidOperationException("A map build is already running.");
			m_buildFormat = formatBuild;
			m_currentItemKey = itemKey;
			m_report = NewReport(formatBuild);
			m_results = null;
			m_provider = new EditorCollectionProvider(formatBuild == FormatBuild.Binary);
			// Failed rebuilds cannot retain old success bits. A format change requires both parts.
			if (MapManagerConfig.Build.format != formatBuild) success = 0;
			success &= ~target;
			SelectCache();
            IsBuilding = true;
            AssetDatabase.DisallowAutoRefresh();
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (IsFormatBlocked(formatBuild, out var reason))
				{
					m_report.Error(SceneValidator.CategoryFormat, reason);
					return;
				}
				InitPath();
				ApplyVendorLimitsToValidation();
				progress?.Report("Checking the map configuration…");
				await Task.Delay(1, cancellationToken);
                fraction?.Invoke(0.02f);
                ValidateMeta();
				if (m_report.HasErrors || !EnsureTargetSceneOpen()) return;
				if (target.HasFlag(TempData.Map))
				{
					ClearDirectory(GetTemporary(TempData.Map));
					progress?.Report("Validating and collecting the scene…");
					await Task.Delay(1, cancellationToken);
                    if (!CollectScene()) return;
					cancellationToken.ThrowIfCancellationRequested();
					progress?.Report("Building the map…");
					await Task.Delay(1, cancellationToken);
                    await ExportMap(value => fraction?.Invoke(0.15f + value * 0.75f), cancellationToken);
					success |= TempData.Map;
				}
				if (target.HasFlag(TempData.Meta))
				{
					cancellationToken.ThrowIfCancellationRequested();
					ClearDirectory(GetTemporary(TempData.Meta));
					progress?.Report("Building the metadata…");
					await Task.Delay(1, cancellationToken);
                    await ExportMeta(value => fraction?.Invoke(0.9f + value * 0.1f), cancellationToken);
					success |= TempData.Meta;
				}
			}
			catch (OperationCanceledException)
			{
				Debug.LogWarning("Build cancelled.");
				success &= ~target;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				success &= ~target;
			}
			finally
			{
                IsBuilding = false;
                AssetDatabase.AllowAutoRefresh();
                UnityGoObjExporter.ClearCache();
				m_results = null;

				progress?.Report(string.Empty);
				PresentReport();
				callback?.Invoke(assetBuildPathTemporary, success);
				MapManagerConfig.SaveForce();
			}
			return;
		}

		private static MapValidationReport NewReport(FormatBuild formatBuild)
		{
			var mapName = MapManagerConfig.Value.mapName;

			return new MapValidationReport
			{
				title = string.IsNullOrWhiteSpace(mapName) ? "<unnamed map>" : mapName,
				sceneName = GetSceneNameFromPathNoId(m_targetScene),
				format = formatBuild,
			};
		}

		public static void ValidateOnly(MapMetaConfig config, FormatBuild formatBuild)
		{
            if (IsBuilding) return;
			if (config == null)
			{
				Debug.LogError("Assign a Map Meta Config before validating.");
				return;
			}

			MapManagerConfig.instance.mapMetaConfigValue = config;
			m_buildFormat = formatBuild;
			m_report = NewReport(formatBuild);

			ApplyVendorLimitsToValidation();

			if (IsFormatBlocked(formatBuild, out var blockReason))
			{
				m_report.Error(SceneValidator.CategoryFormat, blockReason);
			}

			ValidateMeta();

			if (EnsureTargetSceneOpen())
			{
				SceneValidator.Validate(
					m_report,
					SceneManager.GetActiveScene().GetRootGameObjects(),
					formatBuild,
					LastSceneValidation,
					MapSkipComponentConfig.instance.valid);
			}
			else
			{
				m_report.Info("Scene",
					$"'{GetSceneNameFromPathNoId(m_targetScene)}' was not opened, so only the configuration was checked.");
			}

			PresentReport(() => ValidateOnly(config, formatBuild), showWhenClean: true);
		}

		private static bool EnsureTargetSceneOpen()
		{
			if (string.IsNullOrEmpty(m_targetScene))
			{
				return false;
			}

			if (SceneManager.GetActiveScene().path == m_targetScene)
			{
				return true;
			}

			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				return false;
			}

			EditorSceneManager.OpenScene(m_targetScene);
			return true;
		}

	}
}
