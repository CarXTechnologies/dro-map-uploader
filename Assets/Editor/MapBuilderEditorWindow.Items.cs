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
		private async Task FetchItems()
		{
			var session = MapBuilder.session;
			var ready = session.VendorId == "modio"
                ? await session.EnsureInitializedAsync(CancellationToken.None)
                : await session.SelectVendorAsync("modio", CancellationToken.None);

			RefreshVendorBar();

			if (!ready.Success || !session.IsAuthenticated)
			{
				RefreshLocalMaps();
				RefreshAvailability();
				return;
			}

			while (m_buildProcess)
			{
				await Task.Delay(100);
			}

			m_selectItemIndex = -1;
			m_fetchResultListItems.Clear();
			var fetched = await session.Publisher.FetchOwnedItemsAsync(OnItemFetched, CancellationToken.None);

			RefreshAvailability();

			if (!fetched.Success)
			{
				Debug.LogError(fetched.Message);
				return;
			}

			m_fetchResultListItems.Clear();
			m_fetchResultListItems.AddRange(fetched.Value);

			foreach (var item in m_fetchResultListItems)
			{
				m_attaching[item.Key] = MapManagerConfig.IsAttach(item.Key);
			}

			MapManagerConfig.ValidBuildsAndAttaching(session.VendorId, m_fetchResultListItems);
            m_selectItemIndex = m_fetchResultListItems.FindIndex(item => MapManagerConfig.TryGetAttach(item.Key, out var link) && link.metaConfig == m_pendingConfig && m_pendingConfig != null);
			RefreshLocalMaps();
			RefreshItemsList();
			RefreshDetailsPanel();
		}

		private void OnItemFetched(ModItem item)
		{
			// Fetch reports entries as pages arrive, so the row can only be refreshed once the list is rebuilt.
			DownloadSpriteAsync(item);
		}

		private async void DownloadSpriteAsync(ModItem item)
		{
			if (item == null)
			{
				return;
			}

			if (m_images.TryGetValue(item.Key, out var image) && image.downloading)
			{
				return;
			}

			while (m_buildProcess)
			{
				await Task.Delay(100);
			}

			if (image.texture != null)
			{
				DestroyImmediate(image.texture);
			}

			if (string.IsNullOrWhiteSpace(item.PreviewUrl))
			{
				return;
			}

			m_images[item.Key] = (null, true);
			m_loads[item.Key] = true;
			RefreshItemRow(item.Key);

			await UIUtils.DownloadSprite(item.PreviewUrl, (_, texture2D) =>
			{
				m_images[item.Key] = (texture2D == null ? new Texture2D(1, 1) : texture2D, false);
				m_loads[item.Key] = false;
				RefreshItemRow(item.Key);

				if (SelectKey == item.Key)
				{
					RefreshPreview();
				}
			});
		}

		private bool IsDownloadAnyIcon()
		{
			foreach (var item in m_fetchResultListItems)
			{
				if (m_images.TryGetValue(item.Key, out var itemImage) && itemImage.downloading)
				{
					return true;
				}
			}

			return false;
		}

		private void RefreshItemsList()
		{
			if (m_itemsScroll == null)
			{
				return;
			}

			m_itemsScroll.Clear();

			if (m_fetchResultListItems.Count == 0)
			{
				// An empty scroll area reads as "still loading" or "something broke"; say which it is.
				var vendorName = MapBuilder.session.Publisher?.DisplayName ?? "the vendor";
				var hint = new Label(string.Format(L("В {0} пока нет публикаций.\n", "No publications on {0} yet.\n"), vendorName) +
				                     L("Соберите карту и нажмите «Создать публикацию».", "Build the map and click Create publication."));
				hint.AddToClassList("mb-empty-hint");
				m_itemsScroll.Add(hint);
				return;
			}

			for (var i = 0; i < m_fetchResultListItems.Count; i++)
			{
				if (MapManagerConfig.TryGetAttach(m_fetchResultListItems[i].Key, out var linked) && linked.metaConfig != null && linked.metaConfig != m_pendingConfig) continue;
				m_itemsScroll.Add(BuildItemRow(i));
			}
		}

		private VisualElement BuildItemRow(int index)
		{
			var item = m_fetchResultListItems[index];
			var hasOldFlag = item.Tags.Any(MapBuilder.publisherContext.IsLegacyContentTag);

			var row = new VisualElement { userData = item.Key };
			row.AddToClassList("mb-item-row");
			row.AddToClassList(index % 2 == 0 ? "mb-item-row-even" : "mb-item-row-odd");
			if (hasOldFlag)
			{
				row.AddToClassList("mb-item-row-old");
			}

			if (index == m_selectItemIndex)
			{
				row.AddToClassList("mb-item-row-selected");
			}

			row.RegisterCallback<ClickEvent>(_ => OnItemRowClicked(index));

			var thumb = new Image { name = "thumb", scaleMode = ScaleMode.ScaleToFit };
			thumb.AddToClassList("mb-item-thumb");
			row.Add(thumb);

			var info = new VisualElement();
			info.AddToClassList("mb-item-info");

			var title = new Label(string.IsNullOrWhiteSpace(item.Title) ? $"Blank {index}" : item.Title);
			title.AddToClassList("mb-item-title");
			info.Add(title);

			var limits = MapBuilder.session.Limits;
			var maxMb = limits == null ? 0f : limits.MaxPayloadSizeInMb + limits.MaxMetaSizeInMb;
			var size = $"{Mathf.FloorToInt(item.PayloadSizeBytes / MapSceneRules.BytesPerMegabyte)} / {maxMb} mb";

			var sizeLabel = new Label(string.IsNullOrWhiteSpace(item.StatusLabel)
				? size
				: $"{size}  ·  {item.StatusLabel}")
			{
				tooltip = BuildStatusTooltip(item),
			};
			sizeLabel.AddToClassList("mb-item-size");
			info.Add(sizeLabel);

			row.Add(info);

			if (hasOldFlag)
			{
				var oldLabel = new Label("Old version!");
				oldLabel.AddToClassList("mb-item-old-badge");
				row.Add(oldLabel);
			}

			if (!MapManagerConfig.TryGetAttach(item.Key, out var attachData) || attachData.metaConfig == null)
			{
				var warning = new Label(L("Выбрать для этой карты", "Select for this map")) { tooltip = L("Связать эту публикацию с выбранной локальной картой", "Link this publication to the selected local map") };
				warning.AddToClassList("mb-item-warning");
				row.Add(warning);
			}

			ApplyItemThumbnail(row, item.Key);

			return row;
		}

		/// <summary>
		/// Points at the vendor page for the states the vendor keeps to itself. mod.io tracks a review status and a
		/// hidden/public flag that its Unity plugin does not expose, so the page is the only place to read them.
		/// </summary>
		private static string BuildStatusTooltip(ModItem item)
		{
			var url = MapBuilder.session.Publisher?.GetItemUrl(item.Key);
			return string.IsNullOrWhiteSpace(url)
				? item.StatusLabel
				: $"{item.StatusLabel}\nFull status and visibility: {url}";
		}

		private void RefreshItemRow(ModItemKey key)
		{
			if (m_itemsScroll == null)
			{
				return;
			}

			VisualElement existing = null;
			foreach (var child in m_itemsScroll.Children())
			{
				if (child.userData is ModItemKey childKey && childKey == key)
				{
					existing = child;
					break;
				}
			}

			if (existing != null)
			{
				ApplyItemThumbnail(existing, key);
			}
		}

		private void ApplyItemThumbnail(VisualElement row, ModItemKey key)
		{
			var thumb = row.Q<Image>("thumb");
			if (thumb == null)
			{
				return;
			}

			thumb.RemoveFromClassList("mb-item-thumb-loading");

			if (m_images.TryGetValue(key, out var imageData) && !imageData.downloading)
			{
				thumb.image = imageData.texture != null && imageData.texture.width > 1 ? imageData.texture : null;
			}
			else if (m_loads.TryGetValue(key, out var loading) && loading)
			{
				thumb.image = null;
				thumb.AddToClassList("mb-item-thumb-loading");
			}
			else
			{
				thumb.image = null;
			}
		}

		private void TickSpinner()
		{
			if (m_itemsScroll == null)
			{
				return;
			}

			var iconName = "d_WaitSpin" + (Mathf.FloorToInt(Time.realtimeSinceStartup * 12) % 12).ToString("00");
			var icon = EditorGUIUtility.IconContent(iconName).image;

			foreach (var child in m_itemsScroll.Children())
			{
				var thumb = child.Q<Image>("thumb");
				if (thumb != null && thumb.ClassListContains("mb-item-thumb-loading"))
				{
					thumb.image = icon;
				}
			}
		}

		private void OnItemRowClicked(int index)
		{
			if (m_buildProcess || m_pendingConfig == null) return;
			MapManagerConfig.Attach(m_fetchResultListItems[index].Key, m_pendingConfig);
			m_attaching[m_fetchResultListItems[index].Key] = true;
			m_selectItemIndex = index;
			m_buttonLastClickOnAnyItem = true;
			RefreshItemsList();
			RefreshDetailsPanel();
		}
	}
}
