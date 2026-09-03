using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Plugins.CarX.Modding.Creator.Runtime.Publishing;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ModPublisherSession = Plugins.CarX.Modding.Creator.Editor.Publishing.ModPublisherSession;

namespace Editor
{
	public partial class MapBuilderEditorWindow
	{
		private void OnUploadVendorClicked()
		{
			Upload(localBuild: false);
		}

		private void OnUploadLocalClicked()
		{
			Upload(localBuild: true);
		}

		private async void Upload(bool localBuild)
		{
			var item = SelectItem;

			if (item == null || m_buildProcess ||
			    !MapManagerConfig.GetOrAttach(item.Key, out var attachObj) || attachObj == null || attachObj.metaConfig == null)
			{
				return;
			}

			var buildData = MapManagerConfig.GetBuildOrEmpty(attachObj.metaConfig);

			m_loads[item.Key] = true;
			m_buildProcess = true;
			MapManagerConfig.instance.mapMetaConfigValue = attachObj.metaConfig;

			// Publishing runs without a modal progress bar, so this line is the only in-window sign of activity.
			SetPublishStatus(localBuild ? "Copying to the local install folder…" : "Uploading…");

			BeginOperation();
			RefreshDetailsPanel();

			var progress = new Progress<float>(fraction =>
				SetPublishStatus($"Uploading… {Mathf.RoundToInt(Mathf.Clamp01(fraction) * 100f)}%"));

			try
			{
				await MapBuilder.UploadCommunityItem(buildData, item, localBuild, progress, m_operationCts.Token,
					uploadedKey =>
					{
						m_loads[item.Key] = false;
						SetPublishStatus(string.Empty);

						if (uploadedKey.IsValid)
						{
							DownloadSpriteAsync(m_fetchResultListItems.Find(candidate => candidate.Key == uploadedKey));
						}
					});
			}
			finally
			{
				m_loads[item.Key] = false;
				SetPublishStatus(string.Empty);
				EndOperation();
			}
		}

		private void OnBrowseExternalClicked()
		{
			var path = EditorUtility.SaveFolderPanel("External path", Application.streamingAssetsPath, SelectKey.id ?? string.Empty);
			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			m_pathToExternal = path;
			m_externalPathField.SetValueWithoutNotify(m_pathToExternal);
			m_externalExportButton.SetEnabled(!string.IsNullOrWhiteSpace(m_pathToExternal) && Path.IsPathFullyQualified(m_pathToExternal));
		}

		private void OnExportExternalClicked()
		{
			MapManagerConfig.GetOrAttach(SelectKey, out var attachObj);
			var config = attachObj?.metaConfig != null ? attachObj.metaConfig : m_pendingConfig;

			if (config == null)
			{
				SetPublishStatus("Assign a Map Meta Config before exporting.");
				return;
			}

			var buildData = MapManagerConfig.GetBuildOrEmpty(config);

			// The copy is synchronous and writes outside the project, so the status line is the only sign it ran.
			SetPublishStatus(MapBuilder.BuildDataTransitionToDirectory(buildData, m_pathToExternal)
				? $"Exported to {m_pathToExternal}"
				: "Nothing to export — see the Console.");
		}

		private async void OnNewItemClicked()
		{
			// mod.io refuses to create an entry without a name, a summary and a logo, so a config has to be supplied
			// up front. The field above is the source - it works with nothing selected, which is the only way to
			// create the very first item on an account. Steam ignores all of it.
			MapManagerConfig.TryGetAttach(SelectKey, out var attachObj);
			var config = attachObj?.metaConfig != null ? attachObj.metaConfig : m_pendingConfig;

			if (m_buildProcess)
			{
				return;
			}

			m_buildProcess = true;
			BeginOperation();
			SetBuildStatus("Creating the item…");
			RefreshDetailsPanel();

			try
			{
				await MapBuilder.CreateNewCommunityItem(config, m_operationCts.Token,
					newKey => OnItemCreated(config, newKey));
			}
			finally
			{
				EndOperation();
			}
		}

		private void OnItemCreated(MapMetaConfig config, ModItemKey newKey)
		{
			if (config != null && newKey.IsValid)
			{
				MapManagerConfig.Attach(newKey, config);
				m_attaching[newKey] = true;
				InvalidateMetaBuild(config);
			}

			Fetch();
		}

		/// <summary>
		/// Marks the meta as needing a rebuild after an entry is created.
		/// </summary>
		/// <remarks>
		/// The meta carries the mod id, and the build that was attached during creation was necessarily made before
		/// the entry existed - so it holds the map config's id as a placeholder. Clearing the flag makes the build
		/// panel ask for a Meta rebuild through the same warning it already uses when the meta goes stale.
		/// </remarks>
		private static void InvalidateMetaBuild(MapMetaConfig config)
		{
			var buildData = MapManagerConfig.GetBuildOrEmpty(config);

			if (buildData.config == null || !((TempData)buildData.buildSuccess).HasFlag(TempData.Meta))
			{
				return;
			}

			var withoutMeta = (TempData)buildData.buildSuccess & ~TempData.Meta;

			MapManagerConfig.AddBuild(new MapManagerConfig.BuildData(
				config,
				buildData.targetScene,
				buildData.path,
				(int)withoutMeta,
				buildData.lastValid,
				buildData.format,
				buildData.platform,
				buildData.compress));

			Debug.Log("The item was created with the build that existed at the time, whose meta carries a placeholder " +
			          "id. Rebuild Meta and upload so the mod id in the meta matches the item.");
		}

		private async void OnDeleteItemClicked()
		{
			var selected = SelectItem;
			var result = await MapBuilder.session.PromptDeleteAsync(
				selected?.Key ?? default, selected?.Title, CancellationToken.None);

			if (!result.Success)
			{
				Debug.LogError(result.Message);
				return;
			}

			Debug.Log(result.Message);

			// The local attachment outlives the item it pointed at, so it is dropped before the list is rebuilt.
			if (selected != null)
			{
				MapManagerConfig.Detach(selected.Key);
			}

			m_selectItemIndex = 0;
			await FetchItems();
		}

		/// <summary>
		/// Explains what the active vendor still needs before "New Item" can work, rather than letting the button
		/// fail with a message in the console.
		/// </summary>
		private void RefreshNewItemHint()
		{
			if (m_newItemHint == null)
			{
				return;
			}

			// An item is always created together with its content, on every vendor - so the gate is the same
			// everywhere: a config to describe it, and a finished build to publish.
			if (m_pendingConfig == null)
			{
				ShowNewItemHint("Assign a Map Meta Config above to create an item.");
				return;
			}

			var built = (TempData)MapManagerConfig.GetBuildOrEmpty(m_pendingConfig).buildSuccess;
			var missing = (TempData.Map | TempData.Meta) & ~built;

			if (missing != 0)
			{
				ShowNewItemHint($"Build {missing} first — an item is created together with its files.");
				return;
			}

			m_newItemHint.style.display = DisplayStyle.None;
			m_newItemButton?.SetEnabled(true);
		}

		private void ShowNewItemHint(string message)
		{
			m_newItemHint.text = message;
			m_newItemHint.style.display = DisplayStyle.Flex;
			m_newItemButton?.SetEnabled(false);
		}
	}
}
