using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Ugc;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
	public class MapBuilderEditorWindow : EditorWindow
	{
		private const string StyleSheetPath = "Assets/Editor/MapBuilderEditorWindow.uss";

		private SteamUGCManager m_steamUgc;
		private int m_selectItemIndex;
		private readonly List<Item> m_fetchResultListItems = new();

		private Item SelectItem => m_selectItemIndex >= 0 && m_selectItemIndex < m_fetchResultListItems.Count ? m_fetchResultListItems[m_selectItemIndex] : default;

		private readonly Dictionary<ulong, bool> m_loads = new();
		private readonly Dictionary<ulong, bool> m_attaching = new();
		private readonly Dictionary<ulong, (Texture2D texture, bool downloading)> m_images = new();

		private int m_buildType;
		private FormatBuild m_buildFormat;
		private FormatBuild m_buildFormatCached;
		private PlatformBuild m_platformBuild;
		private CompressBuild m_compressBuild;
		private PlatformBuild m_platformBuildCached;
		private CompressBuild m_compressBuildCached;
		private bool m_buildProcess;
		private int m_publishDestination;
		private bool m_buttonLastClickOnAnyItem = true;
		private string m_pathToExternal;

		private enum PublishDestination
		{
			SteamWorkshop = 0,
			LocalTest = 1,
			ExternalFolder = 2,
		}

		private VisualElement m_noSteamBox;
		private VisualElement m_mainLayout;

		private ScrollView m_itemsScroll;

		private Image m_previewImage;
		private HelpBox m_previewMissingBox;
		private Label m_previewIdLabel;

		private Label m_descriptionText;

		private ObjectField m_configField;

		private VisualElement m_buildAndPublishWrapper;

		private VisualElement m_buildSection;
		private EnumFlagsField m_buildTargetsField;
		private Button m_buildButton;
		private DropdownField m_sceneField;
		private string[] m_sceneNames = Array.Empty<string>();
		private string[] m_scenePaths = Array.Empty<string>();
		private EnumField m_platformField;
		private EnumField m_formatField;
		private VisualElement m_compressRow;
		private EnumField m_compressField;

		private VisualElement m_buildResultBox;

		private RadioButtonGroup m_destinationGroup;

		private VisualElement m_steamPanel;
		private Toggle m_uploadNameToggle;
		private Toggle m_uploadDescriptionToggle;
		private Toggle m_uploadPreviewToggle;
		private Button m_uploadSteamButton;

		private VisualElement m_localPanel;
		private HelpBox m_localHelpBox;
		private Button m_localButton;

		private VisualElement m_externalPanel;
		private TextField m_externalPathField;
		private Button m_externalExportButton;

		private IVisualElementScheduledItem m_spinnerSchedule;

		private void OnEnable()
		{
			Clear();
		}

		[MenuItem("Tools/MapBuilder")]
		public static void ShowMyEditor()
		{
			MapBuilderEditorWindow wnd = GetWindow<MapBuilderEditorWindow>();
			wnd.titleContent = new GUIContent("MapBuilder");
			wnd.Fetch();
		}

		public void CreateGUI()
		{
			BuildLayout(rootVisualElement);
			m_spinnerSchedule = rootVisualElement.schedule.Execute(TickSpinner).Every(90);
			Fetch();
		}

		private async void Fetch()
		{
			MapBuilder.InitSteamUgc();
			m_steamUgc = MapBuilder.steamUgc;
			await FetchItems();
		}

		private void OnDisable()
		{
			m_spinnerSchedule?.Pause();
			Clear();
			MapManagerConfig.SaveForce();
			SaveChanges();
		}

		private void OnDestroy()
		{
			Clear();
			MapManagerConfig.SaveForce();
			SaveChanges();
		}

		private void Clear()
		{
			foreach (var image in m_images)
			{
				if (image.Value.texture != null)
				{
					DestroyImmediate(image.Value.texture);
				}
			}
		}

		private async Task FetchItems()
		{
			RefreshSteamAvailability();

			if (!SteamClient.IsValid)
			{
				return;
			}

			while (m_buildProcess)
			{
				await Task.Delay(100);
			}

			await m_steamUgc.GetWorkshopItems(m_fetchResultListItems, OnItemFetched);

			foreach (var item in m_fetchResultListItems)
			{
				m_attaching[item.Id] = MapManagerConfig.IsAttach(item.Id);
			}

			MapManagerConfig.ValidBuildsAndAttaching(m_fetchResultListItems);
			RefreshItemsList();
			RefreshDetailsPanel();
		}

		private void OnItemFetched(Item item)
		{
			RefreshItemRow((ulong)item.Id);
			DownloadSpriteAsync(item);
		}

		private async void DownloadSpriteAsync(Item item)
		{
			if (m_images.TryGetValue(item.Id, out var image) && image.downloading)
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

			if (string.IsNullOrWhiteSpace(item.PreviewImageUrl))
			{
				return;
			}

			m_images[item.Id] = (null, true);
			m_loads[item.Id] = true;
			RefreshItemRow((ulong)item.Id);

			await UIUtils.DownloadSprite(item.PreviewImageUrl, (_, texture2D) =>
			{
				m_images[item.Id] = (texture2D == null ? new Texture2D(1, 1) : texture2D, false);
				m_loads[item.Id] = false;
				RefreshItemRow((ulong)item.Id);

				if (SelectItem.Id == item.Id)
				{
					RefreshPreview();
				}
			});
		}

		private bool IsDownloadAnyIcon()
		{
			foreach (var item in m_fetchResultListItems)
			{
				if (m_images.TryGetValue(item.Id, out var itemImage) && itemImage.downloading)
				{
					return true;
				}
			}

			return false;
		}


		private void BuildLayout(VisualElement root)
		{
			root.Clear();

			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
			if (styleSheet != null)
			{
				root.styleSheets.Add(styleSheet);
			}

			root.AddToClassList("mb-root");

			m_noSteamBox = new HelpBox("Please open Steam and sign in, then reopen this window.", HelpBoxMessageType.Error);
			m_noSteamBox.AddToClassList("mb-no-steam");
			m_noSteamBox.style.display = DisplayStyle.None;
			root.Add(m_noSteamBox);

			m_mainLayout = new VisualElement();
			m_mainLayout.AddToClassList("mb-main-layout");
			root.Add(m_mainLayout);

			m_mainLayout.Add(BuildLeftPanel());
			m_mainLayout.Add(BuildRightPanel());
		}

		private VisualElement BuildLeftPanel()
		{
			var left = new ScrollView(ScrollViewMode.Vertical);
			left.AddToClassList("mb-left-panel");

			left.Add(BuildPreviewBox());
			var versionLabel = new Label(GameVersion.GetFullVersion());
			versionLabel.AddToClassList("mb-version-label");
			left.Add(versionLabel);
			left.Add(BuildDescriptionBox());

			m_configField = new ObjectField("Map Meta Config")
			{
				objectType = typeof(MapMetaConfig),
				allowSceneObjects = false,
			};
			m_configField.AddToClassList("mb-field");
			m_configField.RegisterValueChangedCallback(OnConfigFieldChanged);
			left.Add(m_configField);

			m_buildAndPublishWrapper = new VisualElement();
			m_buildAndPublishWrapper.Add(BuildBuildSection());
			m_buildResultBox = new VisualElement();
			m_buildResultBox.AddToClassList("mb-build-result-box");
			m_buildAndPublishWrapper.Add(m_buildResultBox);
			m_buildAndPublishWrapper.Add(BuildDestinationSection());
			left.Add(m_buildAndPublishWrapper);

			return left;
		}

		private VisualElement BuildPreviewBox()
		{
			var previewBox = new VisualElement();
			previewBox.AddToClassList("mb-preview-box");

			m_previewImage = new Image { scaleMode = ScaleMode.ScaleToFit };
			m_previewImage.AddToClassList("mb-preview-image");
			previewBox.Add(m_previewImage);

			m_previewMissingBox = new HelpBox("Preview is missed", HelpBoxMessageType.Warning);
			m_previewMissingBox.AddToClassList("mb-preview-missing");
			previewBox.Add(m_previewMissingBox);

			var idBadge = new VisualElement();
			idBadge.AddToClassList("mb-id-badge");
			var steamIcon = new Image { image = EditorGUIUtility.IconContent("steam").image };
			steamIcon.AddToClassList("mb-id-badge-icon");
			idBadge.Add(steamIcon);
			m_previewIdLabel = new Label(string.Empty);
			m_previewIdLabel.AddToClassList("mb-id-badge-label");
			idBadge.Add(m_previewIdLabel);
			previewBox.Add(idBadge);

			return previewBox;
		}

		private VisualElement BuildDescriptionBox()
		{
			var box = new VisualElement();
			box.AddToClassList("mb-description-box");

			var header = new Label("Description");
			header.AddToClassList("mb-section-header");
			box.Add(header);

			m_descriptionText = new Label(string.Empty) { enableRichText = false };
			m_descriptionText.AddToClassList("mb-description-text");
			box.Add(m_descriptionText);

			return box;
		}

		private VisualElement BuildBuildSection()
		{
			m_buildSection = new VisualElement();
			m_buildSection.AddToClassList("mb-box");

			var header = new Label("Build Settings");
			header.AddToClassList("mb-section-header");
			m_buildSection.Add(header);

			var targetsRow = new VisualElement();
			targetsRow.AddToClassList("mb-row");

			m_buildTargetsField = new EnumFlagsField("Build Targets", (TempData)0);
			m_buildTargetsField.AddToClassList("mb-field");
			m_buildTargetsField.AddToClassList("mb-grow");
			m_buildTargetsField.RegisterValueChangedCallback(OnBuildTargetsChanged);
			targetsRow.Add(m_buildTargetsField);

			m_buildButton = new Button(OnBuildButtonClicked) { text = "Build", tooltip = "Build the selected targets with the settings above" };
			m_buildButton.AddToClassList("mb-build-button");
			targetsRow.Add(m_buildButton);

			m_buildSection.Add(targetsRow);

			m_sceneField = new DropdownField("Target Scene", new List<string>(), 0);
			m_sceneField.AddToClassList("mb-field");
			m_sceneField.RegisterValueChangedCallback(OnSceneChanged);
			m_buildSection.Add(m_sceneField);

			m_formatField = new EnumField("Format", m_buildFormat);
			m_formatField.AddToClassList("mb-field");
			m_formatField.RegisterValueChangedCallback(evt =>
			{
				m_buildFormat = (FormatBuild)evt.newValue;
				UpdateCompressVisibility();
			});
			m_buildSection.Add(m_formatField);

			m_compressRow = new VisualElement();
			m_compressField = new EnumField("Compression", m_compressBuild);
			m_compressField.AddToClassList("mb-field");
			m_compressField.RegisterValueChangedCallback(evt => m_compressBuild = (CompressBuild)evt.newValue);
			m_compressRow.Add(m_compressField);
			m_buildSection.Add(m_compressRow);

			return m_buildSection;
		}

		private VisualElement BuildDestinationSection()
		{
			var box = new VisualElement();
			box.AddToClassList("mb-box");

			var header = new Label("Destination");
			header.AddToClassList("mb-section-header");
			box.Add(header);

			m_destinationGroup = new RadioButtonGroup(string.Empty, new List<string> { "Steam Workshop", "Local Test", "External Folder" });
			m_destinationGroup.AddToClassList("mb-destination-group");
			m_destinationGroup.SetValueWithoutNotify(m_publishDestination);
			m_destinationGroup.RegisterValueChangedCallback(OnDestinationChanged);
			box.Add(m_destinationGroup);

			var publishHeader = new Label("Publish");
			publishHeader.AddToClassList("mb-section-header");
			box.Add(publishHeader);

			m_steamPanel = new VisualElement();

			m_uploadNameToggle = new Toggle("Upload Name") { tooltip = "Overwrite the workshop item title on upload" };
			m_uploadNameToggle.AddToClassList("mb-field");
			m_uploadNameToggle.RegisterValueChangedCallback(evt => MapManagerConfig.instance.uploadSteamName = evt.newValue);
			m_steamPanel.Add(m_uploadNameToggle);

			m_uploadDescriptionToggle = new Toggle("Upload Description") { tooltip = "Overwrite the workshop item description on upload" };
			m_uploadDescriptionToggle.AddToClassList("mb-field");
			m_uploadDescriptionToggle.RegisterValueChangedCallback(evt => MapManagerConfig.instance.uploadSteamDescription = evt.newValue);
			m_steamPanel.Add(m_uploadDescriptionToggle);

			m_uploadPreviewToggle = new Toggle("Upload Preview") { tooltip = "Overwrite the workshop item preview image on upload" };
			m_uploadPreviewToggle.AddToClassList("mb-field");
			m_uploadPreviewToggle.RegisterValueChangedCallback(evt => MapManagerConfig.instance.uploadSteamPreview = evt.newValue);
			m_steamPanel.Add(m_uploadPreviewToggle);

			m_uploadSteamButton = new Button(OnUploadSteamClicked) { text = "Upload to Steam", tooltip = "Publish this build live to the Steam Workshop item" };
			m_uploadSteamButton.AddToClassList("mb-publish-button");
			m_steamPanel.Add(m_uploadSteamButton);
			box.Add(m_steamPanel);

			m_localPanel = new VisualElement();
			m_localHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
			m_localPanel.Add(m_localHelpBox);

			m_localButton = new Button(OnUploadLocalClicked) { text = "Update Local Test Copy", tooltip = "Copy this build into the subscribed workshop item's local folder, without publishing to Steam" };
			m_localButton.AddToClassList("mb-publish-button");
			m_localPanel.Add(m_localButton);
			box.Add(m_localPanel);

			m_externalPanel = new VisualElement();
			var externalRow = new VisualElement();
			externalRow.AddToClassList("mb-row");

			m_externalPathField = new TextField("External Path");
			m_externalPathField.AddToClassList("mb-field");
			m_externalPathField.AddToClassList("mb-grow");
			m_externalPathField.AddToClassList("mb-external-path-field");
			m_externalPathField.RegisterValueChangedCallback(evt =>
			{
				m_pathToExternal = evt.newValue;
				m_externalExportButton.SetEnabled(!string.IsNullOrWhiteSpace(m_pathToExternal) && Path.IsPathFullyQualified(m_pathToExternal));
			});
			externalRow.Add(m_externalPathField);

			var browseButton = new Button(OnBrowseExternalClicked) { text = "…" };
			browseButton.AddToClassList("mb-icon-button");
			externalRow.Add(browseButton);
			m_externalPanel.Add(externalRow);

			m_externalExportButton = new Button(OnExportExternalClicked) { text = "Export to Folder", tooltip = "Copy this build to any external folder on disk" };
			m_externalExportButton.AddToClassList("mb-publish-button");
			m_externalPanel.Add(m_externalExportButton);
			box.Add(m_externalPanel);

			return box;
		}

		private VisualElement BuildRightPanel()
		{
			var right = new VisualElement();
			right.AddToClassList("mb-right-panel");

			var headerRow = new VisualElement();
			headerRow.AddToClassList("mb-row");
			headerRow.AddToClassList("mb-list-header");

			var header = new Label("Workshop Items");
			header.AddToClassList("mb-section-header");
			header.AddToClassList("mb-grow");
			headerRow.Add(header);

			var fetchButton = new Button(Fetch) { text = "Fetch steam workshop", tooltip = "Reload the list of workshop items from Steam" };
			headerRow.Add(fetchButton);
			right.Add(headerRow);

			m_itemsScroll = new ScrollView(ScrollViewMode.Vertical);
			m_itemsScroll.AddToClassList("mb-items-scroll");
			right.Add(m_itemsScroll);

			var newItemButton = new Button(OnNewWorkshopItemClicked) { text = "New Workshop Item", tooltip = "Create a brand new Steam Workshop item" };
			newItemButton.AddToClassList("mb-new-item-button");
			right.Add(newItemButton);

			return right;
		}

		private void RefreshSteamAvailability()
		{
			var isValid = SteamClient.IsValid;
			m_noSteamBox.style.display = isValid ? DisplayStyle.None : DisplayStyle.Flex;
			m_mainLayout.style.display = isValid ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private void RefreshItemsList()
		{
			m_itemsScroll.Clear();

			for (var i = 0; i < m_fetchResultListItems.Count; i++)
			{
				m_itemsScroll.Add(BuildItemRow(i));
			}
		}

		private VisualElement BuildItemRow(int index)
		{
			var item = m_fetchResultListItems[index];
			var hasOldFlag = item.HasTag(SteamUGCManager.MAP_TAG_OLD);

			var row = new VisualElement { userData = (ulong)item.Id };
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

			var itemDetail = SteamUGCManager.GetItemDetail(item);
			var sizeLabel = new Label($"{Mathf.FloorToInt(itemDetail.FileSize / ModMapTestTool.BYTES_TO_MEGABYTES)} / {(ModMapTestTool.Steam.maxSizeInMb + ModMapTestTool.Steam.maxSizeInMbMeta)} mb");
			sizeLabel.AddToClassList("mb-item-size");
			info.Add(sizeLabel);

			row.Add(info);

			if (hasOldFlag)
			{
				var oldLabel = new Label("Old version!");
				oldLabel.AddToClassList("mb-item-old-badge");
				row.Add(oldLabel);
			}

			if (!MapManagerConfig.TryGetAttach(item.Id, out var attachData) || attachData.metaConfig == null)
			{
				var warning = new Label("Detach") { tooltip = "No MapMetaConfig attached to this workshop item yet" };
				warning.AddToClassList("mb-item-warning");
				row.Add(warning);
			}

			ApplyItemThumbnail(row, item.Id);

			return row;
		}

		private void RefreshItemRow(ulong id)
		{
			if (m_itemsScroll == null)
			{
				return;
			}

			VisualElement existing = null;
			foreach (var child in m_itemsScroll.Children())
			{
				if (child.userData is ulong childId && childId == id)
				{
					existing = child;
					break;
				}
			}

			if (existing != null)
			{
				ApplyItemThumbnail(existing, id);
			}
		}

		private void ApplyItemThumbnail(VisualElement row, ulong id)
		{
			var thumb = row.Q<Image>("thumb");
			if (thumb == null)
			{
				return;
			}

			thumb.RemoveFromClassList("mb-item-thumb-loading");

			if (m_images.TryGetValue(id, out var imageData) && !imageData.downloading)
			{
				thumb.image = imageData.texture != null && imageData.texture.width > 1 ? imageData.texture : null;
			}
			else if (m_loads.TryGetValue(id, out var loading) && loading)
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
			m_selectItemIndex = index;
			m_buttonLastClickOnAnyItem = true;
			RefreshItemsList();
			RefreshDetailsPanel();
		}

		private void RefreshDetailsPanel()
		{
			m_attaching.TryGetValue(SelectItem.Id, out var isSelectAttach);

			MapManagerConfig.GetOrAttach(SelectItem.Id, out var attachObj);
			var buildData = attachObj != null ? MapManagerConfig.GetBuildOrEmpty(attachObj.metaConfig) : default;

			if (attachObj != null && m_buttonLastClickOnAnyItem)
			{
				m_compressBuild = buildData.compress;
				m_platformBuild = buildData.platform;
				m_buildType = buildData.buildSuccess;
				MapManagerConfig.instance.targetScene = buildData.targetScene;
				m_buttonLastClickOnAnyItem = false;
			}

			m_configField.SetValueWithoutNotify(attachObj?.metaConfig);

			RefreshPreview();
			RefreshDescription();

			var hasConfig = attachObj != null && attachObj.metaConfig != null;
			m_buildAndPublishWrapper.style.display = hasConfig ? DisplayStyle.Flex : DisplayStyle.None;

			if (!hasConfig)
			{
				m_buildResultBox.Clear();
				return;
			}

			m_buildAndPublishWrapper.SetEnabled(isSelectAttach);

			m_buildTargetsField.SetValueWithoutNotify((TempData)m_buildType);
			m_formatField.SetValueWithoutNotify(m_buildFormat);
			m_compressField.SetValueWithoutNotify(m_compressBuild);
			UpdateCompressVisibility();

			RefreshSceneDropdown(buildData);

			m_buildButton.SetEnabled(m_buildType != 0 && !IsDownloadAnyIcon());

			var uploadState = RefreshBuildResult(attachObj, buildData);

			var destination = (PublishDestination)m_publishDestination;
			MapManagerConfig.instance.buildLocal = destination == PublishDestination.LocalTest;

			m_uploadNameToggle.SetValueWithoutNotify(MapManagerConfig.instance.uploadSteamName);
			m_uploadDescriptionToggle.SetValueWithoutNotify(MapManagerConfig.instance.uploadSteamDescription);
			m_uploadPreviewToggle.SetValueWithoutNotify(MapManagerConfig.instance.uploadSteamPreview);
			m_uploadSteamButton.SetEnabled(uploadState);

			var existItemDirectory = Directory.Exists(SelectItem.Directory);
			m_localHelpBox.text = existItemDirectory ? "don`t use the map restart" : "need subscribe this item";
			m_localHelpBox.messageType = existItemDirectory ? HelpBoxMessageType.Warning : HelpBoxMessageType.Error;
			m_localButton.SetEnabled(uploadState && existItemDirectory);

			m_externalPathField.SetValueWithoutNotify(m_pathToExternal);
			m_externalExportButton.SetEnabled(!string.IsNullOrWhiteSpace(m_pathToExternal) && Path.IsPathFullyQualified(m_pathToExternal));

			UpdateDestinationPanels();
		}

		private void RefreshPreview()
		{
			var hasConfig = MapManagerConfig.TryGetAttach(SelectItem.Id, out var attachObj) && attachObj.metaConfig != null;

			m_previewImage.style.display = hasConfig ? DisplayStyle.Flex : DisplayStyle.None;
			m_previewMissingBox.style.display = hasConfig ? DisplayStyle.None : DisplayStyle.Flex;

			if (hasConfig)
			{
				m_previewImage.image = attachObj.metaConfig.mapMetaConfigValue.largeIcon;
			}

			m_previewIdLabel.text = SelectItem.Id != 0 ? SelectItem.Id.ToString() : string.Empty;
		}

		private void RefreshDescription()
		{
			if (SelectItem.Id == 0)
			{
				m_descriptionText.text = string.Empty;
				return;
			}

			const int maxChars = 280;
			var description = SelectItem.Description;
			var isEmpty = string.IsNullOrWhiteSpace(description);
			var text = isEmpty ? "No description provided for this workshop item." : description.Trim();

			if (!isEmpty && text.Length > maxChars)
			{
				m_descriptionText.tooltip = text;
				text = text.Substring(0, maxChars) + "…";
			}
			else
			{
				m_descriptionText.tooltip = string.Empty;
			}

			m_descriptionText.text = text;
			m_descriptionText.EnableInClassList("mb-description-text-empty", isEmpty);
		}

		private void RefreshSceneDropdown(MapManagerConfig.BuildData buildData)
		{
			var flagScene = ((TempData)m_buildType).HasFlag(TempData.Map);
			m_sceneField.SetEnabled(flagScene);

			var editorScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
			if (editorScenes.Length == 0)
			{
				m_scenePaths = Array.Empty<string>();
				m_sceneNames = Array.Empty<string>();
				m_sceneField.choices = new List<string>();
				return;
			}

			if (!flagScene && !string.IsNullOrWhiteSpace(buildData.targetScene))
			{
				MapManagerConfig.instance.targetScene = buildData.targetScene;
			}

			m_scenePaths = editorScenes.Select(scene => scene.path).ToArray();
			m_sceneNames = editorScenes.Select(scene => MapBuilder.GetSceneNameFromPathNoId(scene.path)).ToArray();
			m_sceneField.choices = new List<string>(m_sceneNames);

			var index = Array.IndexOf(m_scenePaths, MapManagerConfig.instance.targetScene);
			if (index == -1)
			{
				index = 0;
			}

			m_sceneField.SetValueWithoutNotify(m_sceneNames[index]);
			MapManagerConfig.instance.targetScene = m_scenePaths[index];
		}

		private bool RefreshBuildResult(MapManagerConfig.AttachData attachObj, MapManagerConfig.BuildData buildData)
		{
			m_buildResultBox.Clear();

			if (attachObj == null || attachObj.metaConfig == null)
			{
				return true;
			}

			var uploadState = true;
			var buildNames = Enum.GetNames(typeof(TempData));

			foreach (var buildName in buildNames)
			{
				var has = ((TempData)buildData.buildSuccess).HasFlag((TempData)Enum.Parse(typeof(TempData), buildName));
				uploadState = has && uploadState;

				if (!has)
				{
					AddBuildResultBox(buildName + " is not build", HelpBoxMessageType.Error);
				}
				else if (buildName == nameof(TempData.Meta) && !buildData.lastMeta.Equals(attachObj.metaConfig.mapMetaConfigValue))
				{
					AddBuildResultBox($"Is Changed {buildName}! Please build {buildName}.", HelpBoxMessageType.Warning);
				}

				if (buildName == nameof(TempData.Map))
				{
					var message = buildData.lastValid.ToString();
					if (!string.IsNullOrWhiteSpace(message))
					{
						AddBuildResultBox(message, has ? HelpBoxMessageType.Info : HelpBoxMessageType.Error);
					}
				}
			}

			return uploadState;
		}

		private void AddBuildResultBox(string message, HelpBoxMessageType type)
		{
			var box = new HelpBox(message, type);
			box.AddToClassList("mb-build-result-item");
			m_buildResultBox.Add(box);
		}

		private void UpdateCompressVisibility()
		{
			m_compressRow.style.display = m_buildFormat != FormatBuild.Wavefront ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private void UpdateDestinationPanels()
		{
			var destination = (PublishDestination)m_publishDestination;
			m_steamPanel.style.display = destination == PublishDestination.SteamWorkshop ? DisplayStyle.Flex : DisplayStyle.None;
			m_localPanel.style.display = destination == PublishDestination.LocalTest ? DisplayStyle.Flex : DisplayStyle.None;
			m_externalPanel.style.display = destination == PublishDestination.ExternalFolder ? DisplayStyle.Flex : DisplayStyle.None;
		}

		private void OnConfigFieldChanged(ChangeEvent<UnityEngine.Object> evt)
		{
			if (!MapManagerConfig.GetOrAttach(SelectItem.Id, out var attachObj) || attachObj == null)
			{
				return;
			}

			attachObj.metaConfig = evt.newValue as MapMetaConfig;

			if (attachObj.metaConfig != null && m_attaching.TryGetValue(SelectItem.Id, out var attached) && !attached)
			{
				MapManagerConfig.Attach(SelectItem.Id, attachObj.metaConfig);
				m_attaching[SelectItem.Id] = true;
			}

			RefreshDetailsPanel();
		}

		private void OnBuildTargetsChanged(ChangeEvent<Enum> evt)
		{
			m_buildType = Convert.ToInt32(evt.newValue);
			RefreshDetailsPanel();
		}

		private void OnSceneChanged(ChangeEvent<string> evt)
		{
			var index = Array.IndexOf(m_sceneNames, evt.newValue);
			if (index >= 0 && index < m_scenePaths.Length)
			{
				MapManagerConfig.instance.targetScene = m_scenePaths[index];
			}
		}

		private void OnDestinationChanged(ChangeEvent<int> evt)
		{
			m_publishDestination = evt.newValue;
			UpdateDestinationPanels();
			RefreshDetailsPanel();
		}

		private void OnBuildButtonClicked()
		{
			if (IsDownloadAnyIcon() || !MapManagerConfig.GetOrAttach(SelectItem.Id, out var attachObj) || attachObj == null || attachObj.metaConfig == null)
			{
				return;
			}

			var buildData = MapManagerConfig.GetBuildOrEmpty(attachObj.metaConfig);

			m_loads[SelectItem.Id] = true;
			m_buildProcess = true;
			var selectId = (ulong)SelectItem.Id;
			MapManagerConfig.instance.mapMetaConfigValue = attachObj.metaConfig;
			m_compressBuildCached = m_compressBuild;
			m_platformBuildCached = m_platformBuild;
			m_buildFormatCached = m_buildFormat;

			MapBuilder.BuildCustom((TempData)m_buildType,
				(TempData)buildData.buildSuccess,
				selectId,
				m_buildFormatCached,
				m_compressBuildCached,
				m_platformBuildCached,
				(path, success) => AddBuild(attachObj.metaConfig, buildData, path, success));
		}

		private void AddBuild(MapMetaConfig config,
			MapManagerConfig.BuildData buildData,
			string path,
			TempData complete)
		{
			m_loads[SelectItem.Id] = false;

			if (complete == (TempData.Map | TempData.Meta))
			{
				Debug.Log("Build Complete : Everything");
			}

			buildData.compress = m_compressBuildCached;
			buildData.platform = m_platformBuildCached;

			MapManagerConfig.AddBuild(new MapManagerConfig.BuildData(config,
				MapManagerConfig.instance.targetScene,
				path, (int)complete,
				((TempData)m_buildType).HasFlag(TempData.Map) ? ModMapTestTool.Target : buildData.lastValid,
				m_buildFormat, m_platformBuildCached, m_compressBuildCached));

			m_buildProcess = false;
			RefreshDetailsPanel();
		}

		private void OnUploadSteamClicked()
		{
			if (!MapManagerConfig.GetOrAttach(SelectItem.Id, out var attachObj) || attachObj == null || attachObj.metaConfig == null)
			{
				return;
			}

			var buildData = MapManagerConfig.GetBuildOrEmpty(attachObj.metaConfig);
			var selectedId = SelectItem.Id;

			m_loads[selectedId] = true;
			MapManagerConfig.instance.mapMetaConfigValue = attachObj.metaConfig;
			MapBuilder.UploadSteamCommunityItem(buildData, SelectItem, false, id =>
			{
				m_loads[id] = false;
				DownloadSpriteAsync(m_fetchResultListItems.Find(itemFind => itemFind.Id == id));
			});
		}

		private void OnUploadLocalClicked()
		{
			if (!MapManagerConfig.GetOrAttach(SelectItem.Id, out var attachObj) || attachObj == null || attachObj.metaConfig == null)
			{
				return;
			}

			var buildData = MapManagerConfig.GetBuildOrEmpty(attachObj.metaConfig);
			var selectedId = SelectItem.Id;

			m_loads[selectedId] = true;
			MapManagerConfig.instance.mapMetaConfigValue = attachObj.metaConfig;
			MapBuilder.UploadSteamCommunityItem(buildData, SelectItem, true, id =>
			{
				m_loads[id] = false;
				DownloadSpriteAsync(m_fetchResultListItems.Find(itemFind => itemFind.Id == id));
			});
		}

		private void OnBrowseExternalClicked()
		{
			if (!MapManagerConfig.TryGetAttach(SelectItem.Id, out var attachObj))
			{
				return;
			}

			m_pathToExternal = EditorUtility.SaveFolderPanel("External path", Application.streamingAssetsPath, attachObj.id.ToString());
			m_externalPathField.SetValueWithoutNotify(m_pathToExternal);
			m_externalExportButton.SetEnabled(!string.IsNullOrWhiteSpace(m_pathToExternal) && Path.IsPathFullyQualified(m_pathToExternal));
		}

		private void OnExportExternalClicked()
		{
			if (!MapManagerConfig.GetOrAttach(SelectItem.Id, out var attachObj) || attachObj == null || attachObj.metaConfig == null)
			{
				return;
			}

			var buildData = MapManagerConfig.GetBuildOrEmpty(attachObj.metaConfig);
			MapBuilder.BuildDataTransitionToDirectory(buildData, m_pathToExternal);
		}

		private void OnNewWorkshopItemClicked()
		{
			MapBuilder.CreateNewCommunityFile(result =>
			{
				if (result.Success)
				{
					Fetch();
				}
			});
		}
	}
}