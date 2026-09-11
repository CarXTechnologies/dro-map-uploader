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
	public partial class MapBuilderEditorWindow : EditorWindow
	{
		private const string StyleSheetPath = "Assets/Editor/MapBuilderEditorWindow.uss";

		private int m_selectItemIndex = -1;
		private readonly List<ModItem> m_fetchResultListItems = new();

		private ModItem SelectItem => m_selectItemIndex >= 0 && m_selectItemIndex < m_fetchResultListItems.Count
			? m_fetchResultListItems[m_selectItemIndex]
			: null;

		private ModItemKey SelectKey => SelectItem?.Key ?? default;

		private readonly Dictionary<ModItemKey, bool> m_loads = new();
		private readonly Dictionary<ModItemKey, bool> m_attaching = new();
		private readonly Dictionary<ModItemKey, (Texture2D texture, bool downloading)> m_images = new();

		[SerializeField] private int m_buildType = 3;
		[SerializeField] private FormatBuild m_buildFormat = FormatBuild.Binary;
		private FormatBuild m_buildFormatCached;
		private bool m_buildProcess;
		private bool m_fetching;
		[SerializeField] private PublishDestination m_publishDestination = PublishDestination.ExternalFolder;

		/// <summary>
		/// Destinations offered for the active vendor, in the order they appear in the radio group.
		/// The group works in indices, so this is what maps a click back to a destination - the list is not fixed,
		/// because a vendor that does not install items locally does not get a Local Test option at all.
		/// </summary>
		private readonly List<PublishDestination> m_destinationOptions = new();
		private bool m_buttonLastClickOnAnyItem = true;
		[SerializeField] private string m_pathToExternal;

		private enum PublishDestination
		{
			Vendor = 0,
			LocalTest = 1,
			ExternalFolder = 2,
		}

		private VisualElement m_vendorBar;
		private DropdownField m_vendorField;
		private DropdownField m_gameField;
		private Image m_gamePreview;
		private Texture2D m_gamePreviewTexture;
		private string m_gamePreviewUrl;
		private Label m_authLabel;
		private Button m_authButton;

		private VisualElement m_unavailableBox;
		private HelpBox m_unavailableHelp;
		private VisualElement m_mainLayout;

		private ScrollView m_itemsScroll;

		private Image m_previewImage;
		private HelpBox m_previewMissingBox;
		private Label m_previewIdLabel;



		/// <summary>
		/// Config chosen in the field, kept even while no item is selected. On a vendor account without any items
		/// there is nothing to attach a config to, yet one is still needed to create the first item.
		/// </summary>
		[SerializeField] private MapMetaConfig m_pendingConfig;

		private HelpBox m_newItemHint;
		private Button m_newItemButton;

		private VisualElement m_buildAndPublishWrapper;

		private VisualElement m_buildSection;
        private EnumField m_binaryTexturesField;
		private EnumFlagsField m_buildTargetsField;
		private Button m_buildButton;
		private Button m_validateButton;
		private Button m_cancelButton;
		private Label m_buildStatus;
		private CancellationTokenSource m_operationCts;
		private DropdownField m_sceneField;
		private string[] m_sceneNames = Array.Empty<string>();
		private string[] m_scenePaths = Array.Empty<string>();
		private EnumField m_formatField;
		private HelpBox m_formatBlockBox;

		private VisualElement m_buildResultBox;

		private VisualElement m_destinationSection;
		private HelpBox m_noItemHint;
		private Label m_publishStatus;
		private TextField m_changelogField;
		private RadioButtonGroup m_destinationGroup;

		private VisualElement m_vendorPanel;
		private Toggle m_uploadNameToggle;
		private Toggle m_uploadDescriptionToggle;
		private Toggle m_uploadPreviewToggle;
		private Button m_uploadVendorButton;

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

			// A window opening for the first time fetches from CreateGUI; only an already open one needs a nudge.
			if (wnd.m_itemsScroll != null)
			{
				wnd.Fetch();
			}
		}

		public void CreateGUI()
		{
			BuildLayout(rootVisualElement);
			m_spinnerSchedule = rootVisualElement.schedule.Execute(TickSpinner).Every(90);
			Fetch();
		}

		private async void Fetch()
		{
			// Guarded because a fetch is kicked off from several places - window open, vendor switch, sign in, the
			// Fetch button - and two of them overlapping would duplicate every request the vendor makes.
			if (m_fetching)
			{
				return;
			}

			m_fetching = true;

			try
			{
				await FetchItems();
			}
			finally
			{
				m_fetching = false;
			}
		}

		private void OnDisable()
		{
			m_operationCts?.Cancel();
			m_spinnerSchedule?.Pause();
			Clear();
			MapManagerConfig.SaveForce();
			SaveChanges();
		}

		private void OnDestroy()
		{
			m_operationCts?.Cancel();
			Clear();
			MapManagerConfig.SaveForce();
			SaveChanges();
		}

		private void Clear()
		{
			if (m_gamePreviewTexture != null)
			{
				DestroyImmediate(m_gamePreviewTexture);
				m_gamePreviewTexture = null;
				m_gamePreviewUrl = null;
			}

			foreach (var image in m_images)
			{
				if (image.Value.texture != null)
				{
					DestroyImmediate(image.Value.texture);
				}
			}
		}


		private void BuildLayout(VisualElement root) => BuildWorkspace(root);

		/// <summary>
		/// Vendor picker plus sign in state. Sits above everything else because every other control in the window
		/// only makes sense once a vendor is up and a user is signed in.
		/// </summary>
		private VisualElement BuildVendorBar()
		{
			m_vendorBar = new VisualElement();
			m_vendorBar.AddToClassList("mb-vendor-bar");

			m_vendorField = new DropdownField(L("Площадка", "Platform"), GetVendorDisplayNames(), 0);
			m_vendorField.AddToClassList("mb-field");
			m_vendorField.RegisterValueChangedCallback(OnVendorChanged);
			m_vendorBar.Add(m_vendorField);

			m_gamePreview = new Image { scaleMode = ScaleMode.ScaleToFit };
			m_gamePreview.AddToClassList("mb-game-preview");
			m_gamePreview.style.display = DisplayStyle.None;
			m_vendorBar.Add(m_gamePreview);

			m_gameField = new DropdownField(L("Игра", "Game"), new List<string>(), 0);
			m_gameField.AddToClassList("mb-field");
			m_gameField.AddToClassList("mb-grow");
			m_gameField.RegisterValueChangedCallback(OnGameChanged);
			m_vendorBar.Add(m_gameField);

			m_authLabel = new Label(string.Empty);
			m_authLabel.AddToClassList("mb-auth-state");
			m_vendorBar.Add(m_authLabel);

			m_authButton = new Button(OnAuthButtonClicked) { text = L("Войти", "Sign in") };
			m_vendorBar.Add(m_authButton);

			return m_vendorBar;
		}

		private static List<string> GetVendorDisplayNames()
		{
			return ModPublisherSession.AvailableVendors.Where(vendor => vendor.VendorId == "modio").Select(vendor => vendor.DisplayName).ToList();
		}



		private VisualElement BuildPreviewBox()
		{
			var previewBox = new VisualElement();
			previewBox.AddToClassList("mb-preview-box");

			m_previewImage = new Image { scaleMode = ScaleMode.ScaleToFit };
			m_previewImage.AddToClassList("mb-preview-image");
			previewBox.Add(m_previewImage);

			m_previewMissingBox = new HelpBox(L("Добавьте превью карты", "Add a map preview"), HelpBoxMessageType.Warning);
			m_previewMissingBox.AddToClassList("mb-preview-missing");
			previewBox.Add(m_previewMissingBox);

			var idBadge = new VisualElement();
			idBadge.AddToClassList("mb-id-badge");
			m_previewIdLabel = new Label(string.Empty);
			m_previewIdLabel.AddToClassList("mb-id-badge-label");
			idBadge.Add(m_previewIdLabel);
			previewBox.Add(idBadge);

			return previewBox;
		}



		private VisualElement BuildBuildSection()
		{
			m_buildSection = new VisualElement();
			m_buildSection.AddToClassList("mb-box");

			var header = new Label(L("Сборка карты", "Build map"));
			header.AddToClassList("mb-section-header");
			m_buildSection.Add(header);

			var targetsRow = new VisualElement();
			targetsRow.AddToClassList("mb-row");

			m_buildTargetsField = new EnumFlagsField(L("Что пересобрать", "Rebuild targets"), (TempData)0);
			m_buildTargetsField.AddToClassList("mb-field");
			m_buildTargetsField.AddToClassList("mb-grow");
			m_buildTargetsField.RegisterValueChangedCallback(OnBuildTargetsChanged);
			targetsRow.Add(m_buildTargetsField);

			m_buildSection.Add(targetsRow);

			m_sceneField = new DropdownField(L("Сцена", "Scene"), new List<string>(), 0);
			m_sceneField.AddToClassList("mb-field");
			m_sceneField.RegisterValueChangedCallback(OnSceneChanged);
			m_buildSection.Add(m_sceneField);

			if (MapBuilder.IsFormatBlocked(m_buildFormat, out _)) m_buildFormat = FormatBuild.Binary;
			m_formatField = new EnumField(L("Формат", "Format"), m_buildFormat);
			m_formatField.AddToClassList("mb-field");
			m_formatField.RegisterValueChangedCallback(evt =>
			{
				m_buildFormat = (FormatBuild)evt.newValue;
				UpdateBinaryOptionsVisibility();
				RefreshDetailsPanel();
			});
			m_buildSection.Add(m_formatField);

			m_formatBlockBox = new HelpBox(string.Empty, HelpBoxMessageType.Error);
			m_formatBlockBox.AddToClassList("mb-build-result-item");
			m_formatBlockBox.style.display = DisplayStyle.None;
			m_buildSection.Add(m_formatBlockBox);

            m_binaryTexturesField = new EnumField(L("Текстуры Binary", "Binary textures"), Plugins.CarX.Modding.Creator.Editor.BinaryMapPacker.TextureEncoding)
            {
                tooltip = "BC7: smaller GPU textures with high-quality lossy compression. RGBA32: lossless, larger. Both include mipmaps prepared during export."
            };
            m_binaryTexturesField.AddToClassList("mb-field");
            m_binaryTexturesField.RegisterValueChangedCallback(evt => Plugins.CarX.Modding.Creator.Editor.BinaryMapPacker.TextureEncoding = (Plugins.CarX.Modding.Creator.Runtime.BinaryTextureEncoding)evt.newValue);
            m_buildSection.Add(m_binaryTexturesField);

			var actionsRow = new VisualElement();
			actionsRow.AddToClassList("mb-build-actions");

			m_validateButton = new Button(OnValidateButtonClicked)
			{
				text = L("Проверить карту", "Validate map"),
				tooltip = "Check the map against every rule without building it. Nothing in the scene is modified.",
			};
			m_validateButton.AddToClassList("mb-build-button");
			actionsRow.Add(m_validateButton);

			m_buildButton = new Button(OnBuildButtonClicked) { text = L("Собрать карту", "Build map"), tooltip = "Build the selected targets with the settings above" };
			m_buildButton.AddToClassList("mb-build-button");
			actionsRow.Add(m_buildButton);

			m_cancelButton = new Button(OnCancelButtonClicked) { text = L("Отмена", "Cancel"), tooltip = "Stop the operation in progress" };
			m_cancelButton.AddToClassList("mb-build-button");
			m_cancelButton.style.display = DisplayStyle.None;
			actionsRow.Add(m_cancelButton);

			m_buildSection.Add(actionsRow);

			m_buildStatus = new Label(string.Empty);
			m_buildStatus.AddToClassList("mb-publish-status");
			m_buildStatus.style.display = DisplayStyle.None;
			m_buildSection.Add(m_buildStatus);

            UpdateBinaryOptionsVisibility();

			return m_buildSection;
		}

		private VisualElement BuildDestinationSection()
		{
			var box = new VisualElement();
			box.AddToClassList("mb-box");
			m_destinationSection = box;

			var header = new Label(L("Куда отправить сборку", "Build destination"));
			header.AddToClassList("mb-section-header");
			box.Add(header);

			m_noItemHint = new HelpBox(
				L("Соберите карту, чтобы создать публикацию. Для обновления выберите существующую публикацию ниже.", "Build a map to create a publication. To update one, select an existing publication below."),
				HelpBoxMessageType.Info);
			box.Add(m_noItemHint);

			// Choices are filled in by RefreshDestinationOptions once the active vendor is known.
			m_destinationGroup = new RadioButtonGroup(string.Empty, new List<string>());
			m_destinationGroup.AddToClassList("mb-destination-group");
			m_destinationGroup.RegisterValueChangedCallback(OnDestinationChanged);
			box.Add(m_destinationGroup);

			var publishHeader = new Label(L("Параметры отправки", "Upload settings"));
			publishHeader.AddToClassList("mb-section-header");
			box.Add(publishHeader);

			m_publishStatus = new Label(string.Empty);
			m_publishStatus.AddToClassList("mb-publish-status");
			m_publishStatus.style.display = DisplayStyle.None;
			box.Add(m_publishStatus);

			m_vendorPanel = new VisualElement();

			m_changelogField = new TextField(L("Что изменилось", "Changelog"))
			{
				multiline = true,
				tooltip = "What changed in this release. Shown to players on the mod page.",
			};
			m_changelogField.AddToClassList("mb-field");
			StretchToMultiline(m_changelogField, 54f);
			m_changelogField.RegisterValueChangedCallback(evt => WritePublishNotes(notes => notes.changelog = evt.newValue));
			m_vendorPanel.Add(m_changelogField);

			m_uploadNameToggle = new Toggle(L("Обновить название", "Update title")) { tooltip = "Overwrite the item title on upload" };
			m_uploadNameToggle.AddToClassList("mb-field");
			m_uploadNameToggle.RegisterValueChangedCallback(evt => MapManagerConfig.instance.uploadName = evt.newValue);
			m_vendorPanel.Add(m_uploadNameToggle);

			m_uploadDescriptionToggle = new Toggle(L("Обновить описание", "Update description")) { tooltip = "Overwrite the item description on upload" };
			m_uploadDescriptionToggle.AddToClassList("mb-field");
			m_uploadDescriptionToggle.RegisterValueChangedCallback(evt => MapManagerConfig.instance.uploadDescription = evt.newValue);
			m_vendorPanel.Add(m_uploadDescriptionToggle);

			m_uploadPreviewToggle = new Toggle(L("Обновить превью", "Update preview")) { tooltip = "Overwrite the item preview image on upload" };
			m_uploadPreviewToggle.AddToClassList("mb-field");
			m_uploadPreviewToggle.RegisterValueChangedCallback(evt => MapManagerConfig.instance.uploadPreview = evt.newValue);
			m_vendorPanel.Add(m_uploadPreviewToggle);

			m_uploadVendorButton = new Button(OnUploadVendorClicked) { text = "Upload" };
			m_uploadVendorButton.AddToClassList("mb-publish-button");
			m_vendorPanel.Add(m_uploadVendorButton);
			box.Add(m_vendorPanel);

			m_localPanel = new VisualElement();
			m_localHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
			m_localPanel.Add(m_localHelpBox);

			m_localButton = new Button(OnUploadLocalClicked) { text = L("Обновить локальную копию", "Update local copy"), tooltip = "Copy this build into the item's local install folder, without publishing" };
			m_localButton.AddToClassList("mb-publish-button");
			m_localPanel.Add(m_localButton);
			box.Add(m_localPanel);

			m_externalPanel = new VisualElement();
			var externalRow = new VisualElement();
			externalRow.AddToClassList("mb-row");

			m_externalPathField = new TextField(L("Папка", "Folder"));
			m_externalPathField.AddToClassList("mb-field");
			m_externalPathField.AddToClassList("mb-grow");
			m_externalPathField.AddToClassList("mb-external-path-field");
			m_externalPathField.RegisterValueChangedCallback(evt =>
			{
				m_pathToExternal = evt.newValue;
				RefreshExportButton();
			});
			externalRow.Add(m_externalPathField);

			var browseButton = new Button(OnBrowseExternalClicked) { text = "…" };
			browseButton.AddToClassList("mb-icon-button");
			externalRow.Add(browseButton);
			m_externalPanel.Add(externalRow);

			m_externalExportButton = new Button(OnExportExternalClicked) { text = L("Экспортировать в папку", "Export to folder"), tooltip = "Copy this build to any external folder on disk" };
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

			var header = new Label(L("Публикации", "Publications"));
			header.AddToClassList("mb-section-header");
			header.AddToClassList("mb-grow");
			headerRow.Add(header);

			var fetchButton = new Button(Fetch) { text = L("Обновить список", "Refresh list"), tooltip = "Reload the list of items from the vendor" };
			headerRow.Add(fetchButton);
			right.Add(headerRow);

			m_itemsScroll = new ScrollView(ScrollViewMode.Vertical);
			m_itemsScroll.AddToClassList("mb-items-scroll");
			right.Add(m_itemsScroll);

			m_newItemHint = new HelpBox(string.Empty, HelpBoxMessageType.Info);
			m_newItemHint.style.display = DisplayStyle.None;
			right.Add(m_newItemHint);

			var actionsRow = new VisualElement();
			actionsRow.AddToClassList("mb-row");

			m_newItemButton = new Button(OnNewItemClicked)
			{
				text = L("Создать публикацию", "Create publication"),
				tooltip = "Create a new item on the vendor and publish the finished build to it",
			};
			m_newItemButton.AddToClassList("mb-new-item-button");
			m_newItemButton.AddToClassList("mb-grow");
			actionsRow.Add(m_newItemButton);

			var deleteItemButton = new Button(OnDeleteItemClicked)
			{
				text = L("Удалить публикацию…", "Delete publication…"),
				tooltip = "Delete the selected item from the vendor. With nothing selected you are asked for an id, " +
				          "which is how to remove an item the list cannot show.",
			};
			actionsRow.Add(deleteItemButton);

			right.Add(actionsRow);

			return right;
		}

		private void RefreshVendorBar()
		{
			// Fetch can be kicked off by ShowMyEditor before CreateGUI has built the bar.
			if (m_vendorField == null)
			{
				return;
			}

			var session = MapBuilder.session;
			var vendors = ModPublisherSession.AvailableVendors.Where(vendor => vendor.VendorId == "modio").ToList();

			m_vendorField.choices = vendors.Select(vendor => vendor.DisplayName).ToList();
			m_vendorField.SetEnabled(false);

			var current = vendors.FirstOrDefault(vendor =>
				string.Equals(vendor.VendorId, session.VendorId, StringComparison.OrdinalIgnoreCase));

			if (current != null)
			{
				m_vendorField.SetValueWithoutNotify(current.DisplayName);
			}

			RefreshGamePicker();

			var auth = session.Publisher?.Auth;
			var state = auth?.State ?? ModAuthState.Unavailable(session.Status.Message);

			m_authLabel.text = state.Status switch
			{
				ModAuthStatus.Authenticated => L("Аккаунт: ", "Account: ") + state.UserName,
				ModAuthStatus.Authenticating => L("Вход…", "Signing in…"),
				ModAuthStatus.NotAuthenticated => L("Не выполнен вход", "Not signed in"),
				_ => L("Недоступно", "Unavailable"),
			};

			// Vendors that inherit an ambient session (Steam) have nothing for a button to do.
			var interactive = auth?.RequiresInteractiveLogin ?? false;
			m_authButton.style.display = interactive ? DisplayStyle.Flex : DisplayStyle.None;
			m_authButton.text = state.IsAuthenticated ? L("Выйти", "Sign out") : L("Войти", "Sign in");
			m_authButton.SetEnabled(state.Status != ModAuthStatus.Authenticating);
		}

		private void RefreshAvailability()
		{
			if (m_unavailableBox == null || m_mainLayout == null)
			{
				return;
			}

			var session = MapBuilder.session;
			var usable = session.IsReady && session.IsAuthenticated;

			if (!usable)
			{
				var auth = session.Publisher?.Auth;

				m_unavailableHelp.text = !session.IsReady
					? session.Status.Message
					: string.IsNullOrWhiteSpace(auth?.State.Message)
						? L("Войдите в аккаунт для публикации. Локальный экспорт доступен без входа.", "Sign in to publish. Local export is available without signing in.")
						: auth.State.Message;
			}

			m_unavailableBox.style.display = usable ? DisplayStyle.None : DisplayStyle.Flex;
			m_mainLayout.style.display = DisplayStyle.Flex;
			RefreshDetailsPanel();
		}


		private void RefreshDetailsPanel()
		{
			if (m_metadataFields == null)
			{
				return;
			}

			var key = SelectKey;
			m_attaching.TryGetValue(key, out var isSelectAttach);

			MapManagerConfig.GetOrAttach(key, out var attachObj);

			// The publication links to the local map selected in the library.
			var activeConfig = attachObj?.metaConfig != null ? attachObj.metaConfig : m_pendingConfig;
			var buildData = MapManagerConfig.GetBuildOrEmpty(activeConfig);

			if (attachObj != null && m_buttonLastClickOnAnyItem)
			{
				m_buildType = 3;
				MapManagerConfig.instance.targetScene = buildData.targetScene;
				m_buttonLastClickOnAnyItem = false;
			}

			// Keep the local map selected even when it has no publication yet.
			if (attachObj?.metaConfig != null)
			{
				m_pendingConfig = attachObj.metaConfig;
			}

			RefreshWorkspace(activeConfig, buildData);

			RefreshPreview();
			RefreshNewItemHint();

			var hasConfig = activeConfig != null;
			m_buildAndPublishWrapper.style.display = hasConfig || m_workspaceTab == 0 ? DisplayStyle.Flex : DisplayStyle.None;

			if (!hasConfig)
			{
				m_buildResultBox.Clear();
				m_actionHint.text = L("Выберите локальную карту или добавьте новую.", "Select a local map or add a new one.");
				return;
			}

			// Building is unlocked by the config alone. Requiring a vendor entry here would deadlock mod.io, where an
			// entry cannot be created without a payload to attach - and a payload is what building produces.
			var hasItem = key.IsValid && attachObj?.metaConfig != null && isSelectAttach;
			m_buildSection.SetEnabled(!m_buildProcess);
			m_mapPage.SetEnabled(!m_buildProcess);
			m_localMapsPanel.SetEnabled(!m_buildProcess);
			m_vendorBar.SetEnabled(!m_buildProcess);
			m_destinationSection.SetEnabled(true);

			m_buildTargetsField.SetValueWithoutNotify((TempData)m_buildType);
			m_formatField.SetValueWithoutNotify(m_buildFormat);
			UpdateBinaryOptionsVisibility();

			RefreshSceneDropdown(buildData);

			var formatBlocked = RefreshFormatAvailability();

			m_buildButton.SetEnabled(m_buildType != 0 && !IsDownloadAnyIcon() && !m_buildProcess && !formatBlocked);
			m_validateButton.SetEnabled(!m_buildProcess && !IsDownloadAnyIcon());
			m_cancelButton.style.display = m_buildProcess ? DisplayStyle.Flex : DisplayStyle.None;
			m_cancelButton.SetEnabled(m_buildProcess);

			var uploadState = RefreshBuildResult(activeConfig, buildData);

			RefreshDestinationOptions();
			MapManagerConfig.instance.buildLocal = m_publishDestination == PublishDestination.LocalTest;

			var notes = MapManagerConfig.GetPublishData(activeConfig);
			m_changelogField.SetValueWithoutNotify(notes?.changelog ?? string.Empty);


			m_uploadNameToggle.SetValueWithoutNotify(MapManagerConfig.instance.uploadName);
			m_uploadDescriptionToggle.SetValueWithoutNotify(MapManagerConfig.instance.uploadDescription);
			m_uploadPreviewToggle.SetValueWithoutNotify(MapManagerConfig.instance.uploadPreview);
			m_uploadVendorButton.text = L("Обновить публикацию", "Update publication");
			m_uploadVendorButton.SetEnabled(uploadState && hasItem && !m_buildProcess && MapBuilder.session.IsReady && MapBuilder.session.IsAuthenticated);

			RefreshLocalPanel(uploadState);

			m_externalPathField.SetValueWithoutNotify(m_pathToExternal);
			RefreshExportButton();

			UpdateDestinationPanels();
			var showNoItemHint = !hasItem && m_publishDestination == PublishDestination.Vendor;
			m_noItemHint.style.display = showNoItemHint ? DisplayStyle.Flex : DisplayStyle.None;
			UpdateWorkspaceReadiness(uploadState);
		}

		/// <summary>
		/// Writes a release-notes edit through to the config the panel is currently working on.
		/// Notes live per map rather than per published item, so they are available while creating the first entry
		/// and are not lost when the map is rebuilt.
		/// </summary>
		private void WritePublishNotes(Action<MapManagerConfig.PublishData> edit)
		{
			MapManagerConfig.TryGetAttach(SelectKey, out var attachObj);
			var config = attachObj?.metaConfig != null ? attachObj.metaConfig : m_pendingConfig;

			var notes = MapManagerConfig.GetPublishData(config);
			if (notes == null)
			{
				return;
			}

			edit(notes);
			MapManagerConfig.Save();
		}

		/// <summary>
		/// Gives a multiline <see cref="TextField"/> a real text area instead of the one line box it renders by
		/// default, and lets its text wrap.
		/// </summary>
		/// <remarks>
		/// Done in code rather than in the stylesheet because the element that needs the height is the field's inner
		/// input, which UIElements identifies by the name "unity-text-input" - there is no class of that name, so a
		/// descendant class selector for it silently matches nothing. Wrapping also has to be re-enabled here since
		/// "mb-field" turns it off for the one line fields it is otherwise shared with.
		/// </remarks>
		private static void StretchToMultiline(TextField field, float height)
		{
			field.style.minHeight = height;

			var input = field.Q(TextField.textInputUssName);
			if (input == null)
			{
				return;
			}

			input.style.minHeight = height;
			input.style.whiteSpace = WhiteSpace.Normal;
			input.style.unityTextAlign = TextAnchor.UpperLeft;
		}

		private void SetPublishStatus(string message)
		{
			if (m_publishStatus == null)
			{
				return;
			}

			m_publishStatus.text = message;
			if (!string.IsNullOrEmpty(message)) SetBuildStatus(message);
			m_publishStatus.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
		}

		private void RefreshLocalPanel(bool uploadState)
		{
			// No "this vendor cannot do local installs" branch: RefreshDestinationOptions leaves the option out
			// entirely for those vendors, so this panel is only ever reachable when it applies.
			var installDirectory = SelectItem?.LocalInstallDirectory;

			// The target folder is created on demand, so only a missing game install blocks this - not a missing
			// mods folder, which is exactly what the first local test is supposed to create.
			var resolved = !string.IsNullOrWhiteSpace(installDirectory);

			m_localHelpBox.text = resolved
				? $"Copies the build to {installDirectory}\nClose the game before overwriting a mod it has loaded."
				: "Could not find the game install. Check the Steam app id on SteamWorkshopConfig, " +
				  "and that the game is installed on this machine.";

			m_localHelpBox.messageType = resolved ? HelpBoxMessageType.Info : HelpBoxMessageType.Error;
			m_localButton.SetEnabled(uploadState && resolved && !m_buildProcess);
		}

		private void RefreshPreview()
		{
			// Falls back to the hand picked config so the preview is visible while no item exists yet.
			var config = MapManagerConfig.TryGetAttach(SelectKey, out var attachObj) && attachObj.metaConfig != null
				? attachObj.metaConfig
				: m_pendingConfig;

			var hasConfig = config != null && config.mapMetaConfigValue.largeIcon != null;

			m_previewImage.style.display = hasConfig ? DisplayStyle.Flex : DisplayStyle.None;
			m_previewMissingBox.style.display = hasConfig ? DisplayStyle.None : DisplayStyle.Flex;

			if (hasConfig)
			{
				m_previewImage.image = config.mapMetaConfigValue.largeIcon;
			}

			m_previewIdLabel.text = SelectKey.IsValid ? SelectKey.id : string.Empty;
			m_previewIdLabel.parent.style.display = SelectKey.IsValid ? DisplayStyle.Flex : DisplayStyle.None;
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

		private bool RefreshBuildResult(MapMetaConfig config, MapManagerConfig.BuildData buildData)
		{
			m_buildResultBox.Clear();
			m_sceneContentsText.text = L("Соберите карту, чтобы увидеть состав сцены.", "Build the map to view its scene contents.");

			if (config == null)
			{
				return true;
			}

			var uploadState = true;
			if (buildData.buildSuccess == 3 && !IsSelectedBuildReady())
			{
				uploadState = false;
				AddBuildResultBox(L("Сборка устарела или её файлы недоступны. Соберите выбранную сцену в текущем формате.", "The build is outdated or its files are missing. Build the selected scene in the current format."), HelpBoxMessageType.Warning);
			}
            if (buildData.buildSuccess != 3)
            {
                uploadState = false;
                var missingGeometry = (buildData.buildSuccess & (int)TempData.Map) == 0;
                var missingMetadata = (buildData.buildSuccess & (int)TempData.Meta) == 0;
                AddBuildResultBox(missingGeometry && missingMetadata
                    ? L("Геометрия и метаданные карты ещё не собраны.", "Map geometry and metadata have not been built yet.")
                    : missingGeometry ? L("Геометрия карты ещё не собрана.", "Map geometry has not been built yet.")
                    : L("Метаданные карты ещё не собраны.", "Map metadata has not been built yet."), HelpBoxMessageType.Info);
            }
            AddSceneStats(buildData.lastValid);

			return uploadState;
		}

		private void AddBuildResultBox(string message, HelpBoxMessageType type)
		{
			var box = new HelpBox(message, type);
			box.AddToClassList("mb-build-result-item");
			m_buildResultBox.Add(box);
		}

		private void AddSceneStats(ValidItemData stats)
		{
			var text = stats.ToString();
			m_sceneContentsText.text = string.IsNullOrWhiteSpace(text)
                ? L("Соберите карту, чтобы увидеть состав сцены.", "Build the map to view its scene contents.") : text;
		}

		private void UpdateBinaryOptionsVisibility()
		{
            if (m_binaryTexturesField != null) m_binaryTexturesField.style.display = m_buildFormat == FormatBuild.Binary ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>
		/// Shows why the selected format cannot be built by this editor, if it cannot.
		/// </summary>
		private bool RefreshFormatAvailability()
		{
			var blocked = MapBuilder.IsFormatBlocked(m_buildFormat, out var reason);

			if (m_formatBlockBox != null)
			{
				m_formatBlockBox.text = reason;
				m_formatBlockBox.style.display = blocked ? DisplayStyle.Flex : DisplayStyle.None;
			}

			return blocked;
		}

		/// <summary>
		/// Rebuilds the destination choices for the active vendor, hiding the ones it cannot serve.
		/// </summary>
		private void RefreshDestinationOptions()
		{
			var supportsLocal = MapBuilder.session.Limits?.SupportsLocalInstall ?? false;

			m_destinationOptions.Clear();
			m_destinationOptions.Add(PublishDestination.Vendor);

			if (supportsLocal)
			{
				m_destinationOptions.Add(PublishDestination.LocalTest);
			}

			m_destinationOptions.Add(PublishDestination.ExternalFolder);

			// Switching to a vendor without local installs can strip the destination that was selected.
			if (!m_destinationOptions.Contains(m_publishDestination))
			{
				m_publishDestination = PublishDestination.Vendor;
			}

			var labels = m_destinationOptions.Select(DestinationLabel).ToList();

			if (!m_destinationGroup.choices.SequenceEqual(labels))
			{
				m_destinationGroup.choices = labels;
			}

			m_destinationGroup.SetValueWithoutNotify(m_destinationOptions.IndexOf(m_publishDestination));
		}

		private static string DestinationLabel(PublishDestination destination)
		{
			return destination switch
			{
				PublishDestination.LocalTest => L("Локальная установка", "Local install"),
				PublishDestination.ExternalFolder => L("Экспорт в папку", "Export to folder"),
				_ => L("Площадка", "Platform"),
			};
		}

		private void UpdateDestinationPanels()
		{
			m_vendorPanel.style.display = m_publishDestination == PublishDestination.Vendor ? DisplayStyle.Flex : DisplayStyle.None;
			m_localPanel.style.display = m_publishDestination == PublishDestination.LocalTest ? DisplayStyle.Flex : DisplayStyle.None;
			m_externalPanel.style.display = m_publishDestination == PublishDestination.ExternalFolder ? DisplayStyle.Flex : DisplayStyle.None;
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
				RefreshDetailsPanel();
			}
		}

		private void OnDestinationChanged(ChangeEvent<int> evt)
		{
			if (evt.newValue < 0 || evt.newValue >= m_destinationOptions.Count)
			{
				return;
			}

			m_publishDestination = m_destinationOptions[evt.newValue];
			UpdateDestinationPanels();
			RefreshDetailsPanel();
		}

		private void OnValidateButtonClicked()
		{
			MapManagerConfig.TryGetAttach(SelectKey, out var attachObj);

			var config = attachObj?.metaConfig != null ? attachObj.metaConfig : m_pendingConfig;

			if (config == null || IsDownloadAnyIcon())
			{
				return;
			}

			MapBuilder.ValidateOnly(config, m_buildFormat);
			m_lastOperationSummary = L("Проверка завершена. Результаты — в окне проверки карты.", "Validation finished. See the map validation window for results.");
			SetBuildStatus(string.Empty);
		}

		private async void OnBuildButtonClicked()
		{
            if (MapBuilder.IsBuilding) return;
			var key = SelectKey;

			MapManagerConfig.TryGetAttach(key, out var attachObj);

			// Builds run off the config, with or without a vendor entry: the entry may not exist yet, and on mod.io
			// it cannot be created before there is a build to attach to it.
			var config = attachObj?.metaConfig != null ? attachObj.metaConfig : m_pendingConfig;

			if (IsDownloadAnyIcon() || config == null || m_buildProcess)
			{
				return;
			}

			// Belt and braces on top of the disabled button: a stale panel must not let a locked format through.
			if (MapBuilder.IsFormatBlocked(m_buildFormat, out var blockReason))
			{
				Debug.LogError(blockReason);
				return;
			}

			var buildData = MapManagerConfig.GetBuildOrEmpty(config);

			m_loads[key] = true;
			m_buildProcess = true;
			MapManagerConfig.instance.mapMetaConfigValue = config;
			m_buildFormatCached = m_buildFormat;

			BeginOperation();
			RefreshDetailsPanel();

			try
			{
                await MapBuilder.BuildCustom((TempData)m_buildType,
					(TempData)buildData.buildSuccess,
					key,
					m_buildFormatCached,
					new ImmediateProgress<string>(SetBuildStatus),
					m_operationCts.Token,
					(path, success) => AddBuild(config, buildData, path, success),
                    fraction => { m_operationProgress.value = fraction * 100; m_operationProgress.title = $"{fraction * 100:F0}%"; });
			}
			finally
			{
				EndOperation();
			}
		}

		private void OnCancelButtonClicked()
		{
			if (m_operationCts == null || m_operationCts.IsCancellationRequested)
			{
				return;
			}

			m_operationCts.Cancel();
			SetBuildStatus("Cancelling…");
		}

		private void BeginOperation()
		{
			m_operationCts?.Cancel();
			m_operationCts?.Dispose();
			m_operationCts = new CancellationTokenSource();
			m_operationTimer.Restart();
            m_operationProgress.value = 0;
            m_operationProgress.title = "0%";
            m_operationProgress.style.display = DisplayStyle.Flex;
		}

		private void EndOperation()
		{
			m_operationTimer.Stop();
			m_operationCts?.Dispose();
			m_operationCts = null;

			m_buildProcess = false;
            if (m_operationProgress != null)
            {
                m_operationProgress.value = 0;
                m_operationProgress.title = string.Empty;
                m_operationProgress.style.display = DisplayStyle.None;
            }
			SetBuildStatus(string.Empty);
			RefreshDetailsPanel();
		}

		private void SetBuildStatus(string message)
		{
			if (m_buildStatus == null)
			{
				return;
			}

			m_buildStatus.text = string.IsNullOrEmpty(message) ? m_lastOperationSummary : LocalizeOperation(message);
			m_buildStatus.style.display = string.IsNullOrEmpty(m_buildStatus.text) ? DisplayStyle.None : DisplayStyle.Flex;
		}

		private void AddBuild(MapMetaConfig config,
			MapManagerConfig.BuildData buildData,
			string path,
			TempData complete)
		{
			m_loads[SelectKey] = false;
			RecordBuildResult(config, path, complete);

			if (complete == (TempData.Map | TempData.Meta))
			{
				Debug.Log("Build Complete : Everything");
			}


			MapManagerConfig.AddBuild(new MapManagerConfig.BuildData(config,
				MapManagerConfig.instance.targetScene,
				path, (int)complete,
				((TempData)m_buildType).HasFlag(TempData.Map) ? MapBuilder.LastSceneValidation : buildData.lastValid,
				m_buildFormatCached));

			RefreshDetailsPanel();
		}

	}
}
