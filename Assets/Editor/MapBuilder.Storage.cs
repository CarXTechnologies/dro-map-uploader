using System.IO;
using Plugins.CarX.Modding.Creator.Editor;
using Plugins.CarX.Modding.Creator.Runtime;
using UnityEngine;

namespace Editor
{
	public static partial class MapBuilder
	{
		private static readonly string assetDir = Application.temporaryCachePath + "/";
		private static readonly string assetBuildPath = assetDir + "Standalone";
		private static readonly string assetBuildPathTemporaryOrigin = assetDir + "StandaloneTemporary";
		private static string assetBuildPathTemporary = assetBuildPathTemporaryOrigin;

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

		private static string GetTemporary(TempData name)
		{
			var pathDir = Path.Combine(assetBuildPathTemporary, name + "Temp");
			if (!Directory.Exists(pathDir))
			{
				Directory.CreateDirectory(pathDir);
			}

			return pathDir;
		}

		private static string GetCacheName()
		{
			return MapManagerConfig.instance.mapMetaConfigValue.id;
		}

		private static void SelectCache()
		{
			assetBuildPathTemporary = assetBuildPathTemporaryOrigin + GetCacheName();
		}

		private static void CopyBuildPayload(string directory)
		{
			Directory.CreateDirectory(directory);
			string map = GetTemporary(TempData.Map);
			string meta = GetTemporary(TempData.Meta);
			if (m_buildFormat == FormatBuild.Binary)
			{
				BinaryMapPacker.Pack(Path.Combine(directory, BinaryModArchive.FileName), map, meta);
				return;
			}
			// Switching back to Wavefront must not leave an authoritative stale container.
			string oldContainer = Path.Combine(directory, BinaryModArchive.FileName);
			if (File.Exists(oldContainer)) File.Delete(oldContainer);
			CopyTemporary(map, directory);
			CopyTemporary(meta, directory);
		}

		private static void BuildDataTransition()
		{
			ClearDirectory(assetBuildPath);
			CopyBuildPayload(assetBuildPath);
		}

		private static void BuildDataTransitionLocal(string directory)
		{
			// The game's mods folder may not exist yet, and neither may the mod's own folder inside it - this is the
			// first thing that ever writes there, unlike the old workshop cache which Steam had already created.
			Directory.CreateDirectory(directory);

			ClearDirectory(directory, false);
			CopyBuildPayload(directory);
		}

		private static bool CanStageBuild(MapManagerConfig.BuildData buildData)
		{
			var missing = (TempData.Map | TempData.Meta) & ~(TempData)buildData.buildSuccess;

			if (buildData.config == null || missing != 0 || IsFormatBlocked(buildData.format, out _))
			{
				Debug.LogError(
					$"'{buildData.config?.name ?? "<no config>"}' is not fully built ({missing} missing), so there is " +
					"nothing to export. Build Map and Meta in Wavefront or Binary first.");
				return false;
			}

			return true;
		}

		public static bool BuildDataTransitionToDirectory(MapManagerConfig.BuildData buildData, string directory)
		{
			if (!CanStageBuild(buildData)) return false;

			InitPathUpload(buildData);
			ApplyVendorLimitsToValidation();
			SelectCache();

			CopyBuildPayload(directory);
			return true;
		}

		private static bool IsBuildTooLarge()
		{
			var limits = Limits;

			var mapSizeInMb = GetDirectorySizeInMb(GetTemporary(TempData.Map));
			var metaSizeInMb = GetDirectorySizeInMb(GetTemporary(TempData.Meta));

			var tooLarge = false;

			if (mapSizeInMb > limits.MaxPayloadSizeInMb)
			{
				Debug.LogError($"'{m_uploadScene}': the map is {mapSizeInMb:F2} mb and this vendor accepts " +
				               $"{limits.MaxPayloadSizeInMb} mb.");
				tooLarge = true;
			}

			if (metaSizeInMb > limits.MaxMetaSizeInMb)
			{
				Debug.LogError($"'{m_uploadScene}': the meta is {metaSizeInMb:F2} mb and this vendor accepts " +
				               $"{limits.MaxMetaSizeInMb} mb.");
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

				totalBytes += new FileInfo(file).Length;
			}

			return totalBytes / MapSceneRules.BytesPerMegabyte;
		}
	}
}
