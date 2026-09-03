using System.IO;
using MapUploader.Rules;
using Plugins.CarX.Modding.Creator.Runtime.Publishing;
using UnityEditor;
using UnityEngine;

namespace Editor.Validation
{
	public static class MapMetaValidator
	{
		public const string CategoryMeta = "Meta";

		private const float BytesToMegabytes = 1048576f;
		private const float LargeIconMaxSizeInMb = 10f;

		public static void Validate(MapValidationReport report, MapMetaConfigValue meta, string scenePath,
			ModVendorLimits limits)
		{
			ValidateSceneName(report, scenePath);
			ValidateName(report, meta, limits);
			ValidateDescription(report, meta, limits);
			ValidateSummary(report, meta, limits);
			ValidateIcon(report, meta, limits);
			ValidateLargeIcon(report, meta);
		}

		private static void ValidateSceneName(MapValidationReport report, string scenePath)
		{
			var sceneName = MapMetaRules.GetSceneNameFromPath(scenePath);

			if (string.IsNullOrEmpty(sceneName))
			{
				report.Error(CategoryMeta, "No target scene is selected. Pick one in Build Settings.");
				return;
			}

			if (!MapMetaRules.IsValidSceneName(sceneName))
			{
				report.Error(CategoryMeta,
					$"The scene name '{sceneName}' cannot be used for a build. {MapMetaRules.DescribeSceneNameRule()}");
			}
		}

		private static void ValidateName(MapValidationReport report, MapMetaConfigValue meta, ModVendorLimits limits)
		{
			if (string.IsNullOrWhiteSpace(meta.mapName))
			{
				report.Error(CategoryMeta, "The track has no name. Fill in Workshop Name on the map config.");
				return;
			}

			if (!MapMetaRules.IsValidMapName(meta.mapName))
			{
				report.Error(CategoryMeta,
					$"The track name '{meta.mapName}' contains characters that cannot be published. " +
					MapMetaRules.DescribeMapNameRule());
			}

			if (meta.mapName.Length > limits.MaxTitleLength)
			{
				report.Error(CategoryMeta,
					$"The track name is {meta.mapName.Length} characters; this vendor allows {limits.MaxTitleLength}.");
			}
		}

		private static void ValidateDescription(MapValidationReport report, MapMetaConfigValue meta, ModVendorLimits limits)
		{
			var description = meta.mapDescription ?? string.Empty;

			if (description.Length > limits.MaxDescriptionLength)
			{
				report.Error(CategoryMeta,
					$"The description is {description.Length} characters; this vendor allows {limits.MaxDescriptionLength}.");
			}
		}

		private static void ValidateSummary(MapValidationReport report, MapMetaConfigValue meta, ModVendorLimits limits)
		{
			var summary = MapMetaRules.BuildSummary(meta.mapName, meta.mapDescription, meta.summary);

			if (limits.RequiresSummary && string.IsNullOrWhiteSpace(summary))
			{
				report.Error(CategoryMeta,
					"This vendor requires a summary and there is nothing to build one from. " +
					"Fill in Summary, or the first line of the description.");
				return;
			}

			if (summary.Length > limits.MaxSummaryLength)
			{
				report.Error(CategoryMeta,
					$"The summary is {summary.Length} characters; this vendor allows {limits.MaxSummaryLength}. " +
					(string.IsNullOrWhiteSpace(meta.summary)
						? "It is taken from the first line of the description - add a shorter Summary instead."
						: "Shorten the Summary field."));
			}
		}

		private static void ValidateIcon(MapValidationReport report, MapMetaConfigValue meta, ModVendorLimits limits)
		{
			if (meta.icon == null)
			{
				report.Error(CategoryMeta,
					"No Icon is assigned. It is the image shown for the map in game and on the mod page, and both " +
					"vendors need one before an entry can be created.");
				return;
			}

			if (!meta.icon.isReadable)
			{
				report.Error(CategoryMeta,
					$"The icon '{meta.icon.name}' has Read/Write disabled. Enable it in the texture import settings.",
					meta.icon);
			}

			CheckImageFormat(report, meta.icon, "Icon");
			CheckFileSize(report, meta.icon, "Icon", limits.MaxPreviewSizeInMb);
		}

		private static void ValidateLargeIcon(MapValidationReport report, MapMetaConfigValue meta)
		{
			if (meta.largeIcon == null)
			{
				report.Warning(CategoryMeta,
					"No Preview is assigned. The map will have no large image on the mod page or on the loading screen.");
				return;
			}

			if (!meta.largeIcon.isReadable)
			{
				report.Error(CategoryMeta,
					$"The preview '{meta.largeIcon.name}' has Read/Write disabled. Enable it in the texture import settings.",
					meta.largeIcon);
			}

			CheckImageFormat(report, meta.largeIcon, "Preview");
			CheckFileSize(report, meta.largeIcon, "Preview", LargeIconMaxSizeInMb);
		}

		private static void CheckImageFormat(MapValidationReport report, Texture2D texture, string label)
		{
			var assetPath = AssetDatabase.GetAssetPath(texture);

			if (string.IsNullOrEmpty(assetPath))
			{
				return;
			}

			var extension = Path.GetExtension(assetPath).ToLowerInvariant();

			switch (extension)
			{
				case ".png":
					return;

				case ".jpg":
				case ".jpeg":
					report.Warning(CategoryMeta,
						$"The {label.ToLowerInvariant()} is a {extension} file. The uploader uploads the source file as " +
						"is and the documented format is PNG.",
						texture);
					return;

				default:
					report.Error(CategoryMeta,
						$"The {label.ToLowerInvariant()} is a {extension} file, which is uploaded as is and is not an " +
						"image a vendor accepts. Save it as PNG.",
						texture);
					return;
			}
		}

		private static void CheckFileSize(MapValidationReport report, Texture2D texture, string label, float maxSizeInMb)
		{
			var absolutePath = ToAbsolutePath(AssetDatabase.GetAssetPath(texture));

			if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
			{
				return;
			}

			var sizeInMb = new FileInfo(absolutePath).Length / BytesToMegabytes;

			if (sizeInMb > maxSizeInMb)
			{
				report.Error(CategoryMeta,
					$"The {label.ToLowerInvariant()} is {sizeInMb:F2} mb; the limit is {maxSizeInMb} mb.",
					texture);
			}
		}

		public static string ToAbsolutePath(string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath))
			{
				return string.Empty;
			}

			var projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
			return projectPath + assetPath;
		}
	}
}
