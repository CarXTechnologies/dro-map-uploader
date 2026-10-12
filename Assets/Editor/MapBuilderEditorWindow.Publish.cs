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
		/// <summary>
		/// How often the publish status is redrawn on its own. Uploads report in bursts and processing on the vendor
		/// not at all, so without this the elapsed time would freeze and the window would look stuck.
		/// </summary>
		private const long PublishTickerIntervalMs = 500;

		private IVisualElementScheduledItem m_publishTicker;

		/// <summary>Latest report from the vendor for the running publish operation; null until the first one.</summary>
		private ModUploadProgress? m_publishProgress;

		/// <summary>What the status line says until the vendor's first report - the steps taken before sending.</summary>
		private string m_publishStepText;

		private static string PublishVendorName => MapBuilder.session.Publisher?.DisplayName;

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

			BeginOperation();

			// Publishing runs without a modal progress bar, so the status line and the footer bar are the only
			// in-window sign of activity - the ticker keeps the elapsed time moving even between reports.
			BeginPublishProgress(localBuild
				? L("Копирование в папку локальной установки…", "Copying to the local install folder…")
				: UploadProgressText.StageTitle(ModUploadStage.Preparing, PublishVendorName, English));
			RefreshDetailsPanel();
			var uploaded = false;
			var cancellationToken = m_operationCts.Token;

			try
			{
				// The staging in MapBuilder is synchronous and, with the vendor already initialized, nothing awaits before
				// it - so the status, the bar and Cancel set above would only be painted at the first network request.
				if (!await WaitForRepaint(cancellationToken))
				{
					Debug.LogWarning(localBuild ? "Copying to the local install folder was cancelled." : "Upload cancelled.");
					return;
				}

				await MapBuilder.UploadCommunityItem(buildData, item, localBuild, CreatePublishProgress(), cancellationToken,
					uploadedKey =>
					{
						m_loads[item.Key] = false;
						uploaded = uploadedKey.IsValid;
						SetPublishStatus(string.Empty);

						if (uploadedKey.IsValid)
						{
							DownloadSpriteAsync(m_fetchResultListItems.Find(candidate => candidate.Key == uploadedKey));
						}
					});
			}
			finally
			{
				EndPublishProgress();
				m_loads[item.Key] = false;
				SetPublishStatus(string.Empty);

				// The progress line disappears with the operation, so the footer says how it ended.
				m_lastOperationSummary = !uploaded
					? L("Обновление не завершено. Подробности в Console.", "Update did not complete. See the Console.")
					: localBuild
						? L("Локальная копия обновлена.", "Local copy updated.")
						: L("Публикация обновлена.", "Publication updated.");
				EndOperation();
				if (!localBuild) await FetchItems();
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
			RefreshExportButton();
		}

		private void OnExportExternalClicked()
		{
			MapManagerConfig.GetOrAttach(SelectKey, out var attachObj);
			var config = attachObj?.metaConfig != null ? attachObj.metaConfig : m_pendingConfig;

			if (config == null)
			{
				SetPublishStatus(L("Выберите настройки карты перед экспортом.", "Assign a Map Meta Config before exporting."));
				return;
			}

			if (m_buildProcess || !CanExportCurrentMap()) return;
			var buildData = MapManagerConfig.GetBuildOrEmpty(config);

			// The copy is synchronous and writes outside the project, so the status line is the only sign it ran.
			SetPublishStatus(MapBuilder.BuildDataTransitionToDirectory(buildData, m_pathToExternal)
				? L("Экспортировано в ", "Exported to ") + m_pathToExternal
				: L("Нечего экспортировать — подробности в Console.", "Nothing to export — see the Console."));
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
			BeginPublishProgress(LocalizeOperation("Creating the item…"));
			RefreshDetailsPanel();
			var createdKey = default(ModItemKey);
			var cancellationToken = m_operationCts.Token;

			try
			{
				// Same as in Upload: the build is staged synchronously before anything awaits, so paint first.
				if (!await WaitForRepaint(cancellationToken))
				{
					Debug.LogWarning("Creating the community item was cancelled.");
					return;
				}

				await MapBuilder.CreateNewCommunityItem(config, cancellationToken,
					newKey => createdKey = newKey,
					new ImmediateProgress<string>(ReportPublishStep),
					CreatePublishProgress());
				if (createdKey.IsValid) OnItemCreated(config, createdKey);
			}
			finally
			{
				EndPublishProgress();
				m_lastOperationSummary = createdKey.IsValid
					? L("Публикация создана.", "Publication created.")
					: L("Публикация не завершена. Подробности в Console.", "Publication did not complete. See the Console.");
				EndOperation();
			}
			// Refresh only after releasing the operation guard; fetching waits for it.
			if (createdKey.IsValid) Fetch();
		}

		private void OnItemCreated(MapMetaConfig config, ModItemKey newKey)
		{
			if (config != null && newKey.IsValid)
			{
				MapManagerConfig.Attach(newKey, config);
				m_attaching[newKey] = true;
				InvalidateMetaBuild(config);
			}

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
				buildData.format));

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
				ShowNewItemHint(L("Выберите локальную карту.", "Select a local map."));
				return;
			}

			var built = (TempData)MapManagerConfig.GetBuildOrEmpty(m_pendingConfig).buildSuccess;
			var missing = (TempData.Map | TempData.Meta) & ~built;

			if (missing != 0)
			{
				ShowNewItemHint(L("Сначала соберите карту и её данные. Публикация создаётся вместе с файлами.", "Build the map and its metadata first. A publication is created together with its files."));
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

		/// <summary>
		/// Starts drawing a publish operation: <paramref name="stepText"/> until the vendor reports, and the elapsed
		/// time ticking under it either way. Call after <see cref="BeginOperation"/>, which restarts the timer, and
		/// pair with <see cref="EndPublishProgress"/> in a finally.
		/// </summary>
		private void BeginPublishProgress(string stepText)
		{
			m_publishProgress = null;
			m_publishStepText = stepText;

			m_publishTicker?.Pause();
			m_publishTicker = rootVisualElement.schedule.Execute(RenderPublishProgress).Every(PublishTickerIntervalMs);
			RefreshCancelButton();
			RenderPublishProgress();
		}

		private void EndPublishProgress()
		{
			m_publishTicker?.Pause();
			m_publishTicker = null;
			m_publishProgress = null;
			m_publishStepText = null;
			// Unlocks Cancel and restores its tooltip; EndOperation hides it right after.
			RefreshCancelButton();
			SetPublishStatus(string.Empty);
		}

		/// <summary>
		/// Gives the window a chance to paint before synchronous work blocks the main thread. Completes after two editor
		/// updates - the first lets the panel lay out what was just set, the second comes after the repaint that follows -
		/// or as soon as <paramref name="cancellationToken"/> fires: Cancel, or the window closing, which cancels the
		/// running operation in OnDisable.
		/// </summary>
		/// <returns>False when the wait ended by cancellation, so the caller skips the work it was about to start.</returns>
		private static Task<bool> WaitForRepaint(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromResult(false);
			}

			// Asynchronous continuations: completing from inside EditorApplication.update or from Cancel() must not run
			// the caller's synchronous staging (or its cleanup, which disposes the token source) inline.
			var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var updates = 0;
			var registration = default(CancellationTokenRegistration);

			void Finish(bool painted)
			{
				EditorApplication.update -= OnUpdate;
				registration.Dispose();
				completion.TrySetResult(painted);
			}

			void OnUpdate()
			{
				if (++updates >= 2)
				{
					Finish(true);
				}
			}

			EditorApplication.update += OnUpdate;
			registration = cancellationToken.Register(() => Finish(false));
			return completion.Task;
		}

		/// <summary>
		/// The sink handed to the vendor for the operation that is running now.
		/// </summary>
		private IProgress<ModUploadProgress> CreatePublishProgress()
		{
			var operation = m_operationCts;

			// Progress<T> posts to the main thread, so a report can land after its operation ended - or after the next
			// one began. Both are dropped: only the operation that created this sink may draw.
			return new Progress<ModUploadProgress>(report =>
			{
				if (!m_buildProcess || m_operationCts == null || m_operationCts != operation) return;
				var first = !m_publishProgress.HasValue;
				m_publishProgress = report;

				// The vendor is sending from here on and cannot be stopped, so Cancel is locked for the rest of it.
				if (first) RefreshCancelButton();
				RenderPublishProgress();
			});
		}

		/// <summary>
		/// A step named by <see cref="MapBuilder.CreateNewCommunityItem"/> before anything is sent. Shown until the
		/// vendor's own reports take over.
		/// </summary>
		private void ReportPublishStep(string step)
		{
			if (!m_buildProcess || string.IsNullOrEmpty(step)) return;
			m_publishStepText = LocalizeOperation(step);
			RenderPublishProgress();
		}

		/// <summary>
		/// Draws the running publish operation into the status lines and the footer bar. Called on every report and by
		/// the ticker, so it has to be cheap and safe to call when nothing changed.
		/// </summary>
		private void RenderPublishProgress()
		{
			if (!m_buildProcess || m_operationProgress == null) return;

			var elapsed = m_operationTimer.Elapsed;
			string title;
			string status;

			if (m_publishProgress is { } progress)
			{
				// Checked before cancellation: once the vendor reports it is sending, a Cancel pressed earlier no longer
				// stops anything, so the live progress stays on screen instead of "Cancelling…" for the rest of it.
				var vendor = PublishVendorName;
				title = UploadProgressText.StageTitle(progress.Stage, vendor, English);
				status = UploadProgressText.Format(progress, elapsed, vendor, English);

				// Overall progress is meant to never go back; holding the bar still costs less than trusting that.
				m_operationProgress.value = Mathf.Max(m_operationProgress.value, UploadProgressText.BarValue(progress));
			}
			else if (m_operationCts != null && m_operationCts.IsCancellationRequested)
			{
				// Otherwise the next tick would paint the step text back over "Cancelling…".
				title = LocalizeOperation("Cancelling…");
				status = UploadProgressText.WithElapsed(title, elapsed);
			}
			else
			{
				title = m_publishStepText ?? string.Empty;
				status = UploadProgressText.WithElapsed(m_publishStepText, elapsed);
			}

			m_operationProgress.title = title;
			SetPublishStatus(status);
		}
	}
}
