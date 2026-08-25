using System;
using System.Collections.Generic;
using System.IO;
using Plugins.CarX.Modding.Creator.Runtime.Publishing;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[Flags]
public enum TempData : int
{
	Meta = 1,
	Map = 2,
}

public enum FormatBuild
{
	dro1,
	dro2,
}

public enum PlatformBuild : int
{
	StandaloneWindows = 0,
	//Switch = 1,
	//PS4 = 100,
	//PS5 = 101,
	//XboxOne = 1004,
	//XboxSeries = 1005,
}

public enum CompressBuild : int
{
	NoCompress = 0,
	Compress = 10,
}

[CreateAssetMenu(menuName = "Map/MapManagerConfig", fileName = "MapManagerConfig", order = 0)]
public class MapManagerConfig : SingletonScriptableObject<MapManagerConfig>
{
	[InspectorSetting(isLock: true)] public MapMetaConfig mapMetaConfigValue;
	[InspectorSetting(isLock: true)] public List<AttachData> attachingConfigs = new();
	[InspectorSetting(isLock: true)] public List<BuildData> builds = new();
	[InspectorSetting(isLock: true)] public List<PublishData> publishNotes = new();
	[HideInInspector] public string targetScene;

	[FormerlySerializedAs("uploadSteamName")] [HideInInspector] public bool uploadName;
	[FormerlySerializedAs("uploadSteamDescription")] [HideInInspector] public bool uploadDescription;
	[FormerlySerializedAs("uploadSteamPreview")] [HideInInspector] public bool uploadPreview;

	[HideInInspector] public bool buildLocal;

	/// <summary>
	/// Link between a mod entry on a vendor and the map config that produces its content.
	/// </summary>
	[Serializable]
	public class AttachData
	{
		/// <summary>
		/// Steam published file id from before the uploader supported more than one vendor.
		/// Kept only so that existing configs can be migrated in <see cref="MigrateLegacyAttachments"/>; new entries
		/// leave it at zero and use <see cref="key"/> instead.
		/// </summary>
		[HideInInspector] public ulong id;

		public ModItemKey key;
		public MapMetaConfig metaConfig;
	}

	/// <summary>
	/// Release notes the author writes for a map, kept per map rather than per published item.
	/// They are needed before an item exists - mod.io attaches the file while creating the entry - and they have to
	/// survive a rebuild, which rules out both <see cref="AttachData"/> and <see cref="BuildData"/>.
	/// </summary>
	[Serializable]
	public class PublishData
	{
		public string configId;
		public string version;
		public string changelog;
	}

	[Serializable]
	public struct BuildData
	{
		public MapMetaConfig config;
		public string path;
		public int buildSuccess;
		public ValidItemData lastValid;
		public MapMetaConfigValue lastMeta;
		public FormatBuild format;
		public PlatformBuild platform;
		public CompressBuild compress;
		public string targetScene;

		public BuildData(MapMetaConfig config,
			string targetScene,
			string path,
			int buildSuccess,
			ValidItemData lastValid,
			FormatBuild format,
			PlatformBuild platform,
			CompressBuild compress)
		{
			this.config = config;
			this.path = path;
			this.buildSuccess = buildSuccess;
			this.lastValid = (ValidItemData)lastValid.Clone();
			lastMeta = config.mapMetaConfigValue;
			this.platform = platform;
			this.compress = compress;
			this.targetScene = targetScene;
			this.format = format;
		}
	}

	protected override void OnCreate()
	{
		base.OnCreate();
		MigrateLegacyAttachments();
#if UNITY_EDITOR
		EditorUtility.SetDirty(instance);
		Save();
#endif
	}

	/// <summary>
	/// Rewrites attachments saved before the vendor split, which stored a bare Steam published file id.
	/// Everything published back then went to the Steam Workshop, so that is what they are attributed to.
	/// </summary>
	private void MigrateLegacyAttachments()
	{
		var migrated = false;

		foreach (var attach in attachingConfigs)
		{
			if (attach == null || attach.id == 0 || attach.key.IsValid)
			{
				continue;
			}

			attach.key = new ModItemKey(SteamWorkshopConfig.VendorId, attach.id.ToString());
			migrated = true;
		}

		if (migrated)
		{
			Debug.Log("MapManagerConfig: migrated legacy Steam attachments to the vendor aware format.");
		}
	}

	/// <summary>
	/// Drops attachments whose entry no longer exists on the vendor, and builds whose map config was deleted.
	/// </summary>
	/// <param name="vendorId">
	/// Only attachments of this vendor are considered. Entries belonging to another vendor are left alone - they are
	/// simply not part of the list that was just fetched, not gone.
	/// </param>
	public static void ValidBuildsAndAttaching(string vendorId, IReadOnlyList<ModItem> validationBuilds)
	{
		if (validationBuilds == null || validationBuilds.Count < 1)
		{
			return;
		}

		// Both loops walk backwards: removing while walking forwards skips the element that slides into the gap.
		for (var index = instance.attachingConfigs.Count - 1; index >= 0; index--)
		{
			var attach = instance.attachingConfigs[index];

			if (attach == null || !string.Equals(attach.key.vendor, vendorId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (!Contains(validationBuilds, attach.key))
			{
				instance.attachingConfigs.RemoveAt(index);
			}
		}

		for (var index = instance.builds.Count - 1; index >= 0; index--)
		{
			if (instance.builds[index].config == null)
			{
				ClearDirectory(instance.builds[index].path);
				instance.builds.RemoveAt(index);
			}
		}

		SaveForce();
	}

	private static bool Contains(IReadOnlyList<ModItem> items, ModItemKey key)
	{
		foreach (var item in items)
		{
			if (item.Key == key)
			{
				return true;
			}
		}

		return false;
	}

	public static void AddBuild(BuildData buildData)
	{
		var index = FindIndexBuild(buildData.config);
		if (index == -1)
		{
			instance.builds.Add(buildData);
			Save();
			return;
		}

		instance.builds[index] = buildData;
		Save();
	}

	public static BuildData GetBuildOrEmpty(MapMetaConfig config)
	{
		if (config == null)
		{
			return default;
		}

		var result = instance.builds.Find(b => b.config != null && b.config.id == config.id);
		return result;
	}

	private static int FindIndexBuild(MapMetaConfig config)
	{
		if (config == null)
		{
			return -1;
		}

		var result = instance.builds.FindIndex(b => b.config != null && b.config.id == config.id);
		return result;
	}

	public static void ClearBuild(MapMetaConfig config)
	{
		var buildIndex = FindIndexBuild(config);
		if (buildIndex == -1)
		{
			return;
		}

		ClearDirectory(instance.builds[buildIndex].path);
		instance.builds.RemoveAt(buildIndex);
		Save();
	}

	private static void ClearDirectory(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		if (Directory.Exists(path))
		{
			Directory.Delete(path, true);
		}

		Directory.CreateDirectory(path);
	}

	public static MapMetaConfigValue Value =>
		instance.mapMetaConfigValue.mapMetaConfigValue;

	public static BuildData Build =>
		GetBuildOrEmpty(instance.mapMetaConfigValue);

	/// <summary>Release notes for a map, created empty on first use so the caller can always write through them.</summary>
	public static PublishData GetPublishData(MapMetaConfig config)
	{
		if (config == null || string.IsNullOrEmpty(config.id))
		{
			return null;
		}

		var data = instance.publishNotes.Find(notes => notes.configId == config.id);

		if (data == null)
		{
			data = new PublishData { configId = config.id, version = string.Empty, changelog = string.Empty };
			instance.publishNotes.Add(data);
		}

		return data;
	}

	public static bool IsAttach(ModItemKey key)
	{
		return instance.attachingConfigs.Exists(data => data.key == key && data.metaConfig != null);
	}

	public static AttachData GetAttach(ModItemKey key)
	{
		return instance.attachingConfigs.Find(data => data.key == key);
	}

	public static bool TryGetAttach(ModItemKey key, out AttachData attachData)
	{
		attachData = GetAttach(key);
		return attachData != null;
	}

	public static bool GetOrAttach(ModItemKey key, out AttachData attachData)
	{
		attachData = GetAttach(key) ?? Attach(key, null);
		return true;
	}

	public static AttachData Attach(ModItemKey key, MapMetaConfig config)
	{
		if (!key.IsValid)
		{
			return null;
		}

		if (TryGetAttach(key, out var attachData))
		{
			attachData.metaConfig = config;
		}
		else
		{
			attachData = new AttachData
			{
				key = key,
				metaConfig = config
			};

			instance.attachingConfigs.Add(attachData);
		}

		Save();
		return attachData;
	}

	public static void Detach(ModItemKey key)
	{
		var index = instance.attachingConfigs.FindIndex(data => data.key == key);
		if (index != -1)
		{
			instance.attachingConfigs[index].metaConfig = null;
		}

		Save();
	}

	public static void Save()
	{
#if UNITY_EDITOR
		AssetDatabase.SaveAssetIfDirty(instance);
		AssetDatabase.Refresh();
#endif
	}

	public static void SaveForce()
	{
#if UNITY_EDITOR
		EditorUtility.SetDirty(instance);
		AssetDatabase.SaveAssetIfDirty(instance);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		EditorUtility.FocusProjectWindow();
#endif
	}
}
