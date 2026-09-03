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
		private async void OnVendorChanged(ChangeEvent<string> evt)
		{
			var vendor = ModPublisherSession.AvailableVendors
				.FirstOrDefault(candidate => candidate.DisplayName == evt.newValue);

			if (vendor == null)
			{
				return;
			}

			// Entries belong to the vendor they were fetched from, so nothing from the old list survives the switch.
			m_fetchResultListItems.Clear();
			m_selectItemIndex = 0;
			RefreshItemsList();

			await MapBuilder.session.SelectVendorAsync(vendor.VendorId, CancellationToken.None);
			await FetchItems();
		}

		/// <summary>
		/// Fills the game picker from the active vendor. The list is authored per vendor, so it changes completely
		/// when the vendor does.
		/// </summary>
		private void RefreshGamePicker()
		{
			var options = MapBuilder.session.Publisher?.GameOptions;
			var hasOptions = options is { Count: > 0 };

			m_gameField.style.display = hasOptions ? DisplayStyle.Flex : DisplayStyle.None;

			if (!hasOptions)
			{
				ClearGameSelection();
				return;
			}

			// An entry that is missing credentials is still listed, marked, so it can be selected and fixed rather
			// than silently vanishing from the picker.
			var labels = options
				.Select(option => option.IsConfigured ? option.DisplayName : $"{option.DisplayName}  (incomplete)")
				.ToList();

			if (!m_gameField.choices.SequenceEqual(labels))
			{
				m_gameField.choices = labels;
			}

			var selected = MapBuilder.session.Publisher.SelectedGameIndex;

			if (selected >= 0 && selected < labels.Count)
			{
				m_gameField.SetValueWithoutNotify(labels[selected]);
				m_gameField.tooltip = $"Id {options[selected].Id}";
				SetGamePreview(options[selected].PreviewUrl);
			}
			else
			{
				ClearGameSelection();
			}
		}

		private void ClearGameSelection()
		{
			m_gameField.SetValueWithoutNotify(string.Empty);
			m_gameField.tooltip = string.Empty;
			SetGamePreview(string.Empty);
		}

		private async void SetGamePreview(string url)
		{
			if (m_gamePreviewUrl == url)
			{
				return;
			}

			m_gamePreviewUrl = url;

			if (string.IsNullOrWhiteSpace(url))
			{
				m_gamePreview.style.display = DisplayStyle.None;
				return;
			}

			await UIUtils.DownloadSprite(url, (_, texture) =>
			{
				// The url may have moved on while the download was in flight.
				if (m_gamePreview == null || m_gamePreviewUrl != url)
				{
					return;
				}

				if (m_gamePreviewTexture != null)
				{
					DestroyImmediate(m_gamePreviewTexture);
				}

				m_gamePreviewTexture = texture;
				m_gamePreview.image = texture;
				m_gamePreview.style.display = texture != null ? DisplayStyle.Flex : DisplayStyle.None;
			});
		}

		private async void OnGameChanged(ChangeEvent<string> evt)
		{
			var index = m_gameField.choices.IndexOf(evt.newValue);

			if (index < 0)
			{
				return;
			}

			// Entries belong to the game they were fetched from, so nothing from the old list survives the switch.
			m_fetchResultListItems.Clear();
			m_selectItemIndex = 0;
			RefreshItemsList();

			var result = await MapBuilder.session.SelectGameAsync(index, CancellationToken.None);

			if (!result.Success)
			{
				Debug.LogError(result.Message);
			}
			else if (!string.IsNullOrWhiteSpace(result.Message))
			{
				Debug.LogWarning(result.Message);
			}

			Fetch();
		}

		private async void OnAuthButtonClicked()
		{
			var session = MapBuilder.session;

			var result = session.IsAuthenticated
				? await session.LogoutAsync()
				: await session.LoginAsync(CancellationToken.None);

			if (!result.Success)
			{
				Debug.LogError(result.Message);
			}
			else if (!string.IsNullOrWhiteSpace(result.Message))
			{
				Debug.Log(result.Message);
			}

			RefreshVendorBar();
			await FetchItems();
		}
	}
}
