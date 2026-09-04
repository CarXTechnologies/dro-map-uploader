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

		private int m_selectItemIndex;
		private readonly List<ModItem> m_fetchResultListItems = new();

		private ModItem SelectItem => m_selectItemIndex >= 0 && m_selectItemIndex < m_fetchResultListItems.Count
			? m_fetchResultListItems[m_selectItemIndex]
			: null;

		private ModItemKey SelectKey => SelectItem?.Key ?? default;

		private readonly Dictionary<ModItemKey, bool> m_loads = new();
		private readonly Dictionary<ModItemKey, bool> m_attaching = new();
		private readonly Dictionary<ModItemKey, (Texture2D texture, bool downloading)> m_images = new();

		private int m_buildType;
		private FormatBuild m_buildFormat;
		private FormatBuild m_buildFormatCached;
		private PlatformBuild m_platformBuild;
		private CompressBuild m_compressBuild;
		private PlatformBuild m_platformBuildCached;
		private CompressBuild m_compressBuildCached;
		private bool m_buildProcess;
		private bool m_fetching;
		private PublishDestination m_publishDestination;

		/// <summary>
		/// Destinations offered for the active vendor, in the order they appear in the radio group.
		/// The group works in indices, so this is what maps a click back to a destination - the list is not fixed,
		/// because a vendor that does not install items locally does not get a Local Test option at all.
		/// </summary>
		private readonly List<PublishDestination> m_destinationOptions = new();
		private bool m_buttonLastClickOnAnyItem = true;
		private string m_pathToExternal;

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

		private Label m_descriptionText;

		private ObjectField m_configField;

		/// <summary>
		/// Config chosen in the field, kept even while no item is selected. On a vendor account without any items
		/// there is nothing to attach a config to, yet one is still needed to create the first item.
		/// </summary>
		private MapMetaConfig m_pendingConfig;

		private HelpBox m_newItemHint;
		private Button m_newItemButton;

		private VisualElement m_buildAndPublishWrapper;

		private VisualElement m_buildSection;
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
		private VisualElement m_compressRow;
		private EnumField m_compressField;

		private VisualElement m_buildResultBox;

		private VisualElement m_destinationSection;
		private HelpBox m_noItemHint;
		private Label m_publishStatus;
		private TextField m_versionField;
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


		private void BuildLayout(VisualElement root)
		{
			root.Clear();

			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
			if (styleSheet != null)
			{
				root.styleSheets.Add(styleSheet);
			}

			root.AddToClassList("mb-root");

			root.Add(BuildVendorBar());

			m_unavailableBox = new VisualElement();
			m_unavailableHelp = new HelpBox(string.Empty, HelpBoxMessageType.Error);
			m_unavailableHelp.AddToClassList("mb-unavailable");
			m_unavailableBox.Add(m_unavailableHelp);
			m_unavailableBox.style.display = DisplayStyle.None;
			root.Add(m_unavailableBox);

			m_mainLayout = new VisualElement();
			m_mainLayout.AddToClassList("mb-main-layout");
			root.Add(m_mainLayout);

			m_mainLayout.Add(BuildLeftPanel());
			m_mainLayout.Add(BuildRightPanel());
		}

		/// <summary>
		/// Vendor picker plus sign in state. Sits above everything else because every other control in the window
		/// only makes sense once a vendor is up and a user is signed in.
		/// </summary>
		private VisualElement BuildVendorBar()
		{
			m_vendorBar = new VisualElement();
			m_vendorBar.AddToClassList("mb-vendor-bar");

			m_vendorField = new DropdownField("Vendor", GetVendorDisplayNames(), 0);
			m_vendorField.AddToClassList("mb-field");
			m_vendorField.RegisterValueChangedCallback(OnVendorChanged);
			m_vendorBar.Add(m_vendorField);

			m_gamePreview = new Image { scaleMode = ScaleMode.ScaleToFit };
			m_gamePreview.AddToClassList("mb-game-preview");
			m_gamePreview.style.display = DisplayStyle.None;
			m_vendorBar.Add(m_gamePreview);

			m_gameField = new DropdownField("Game", new List<string>(), 0);
			m_gameField.AddToClassList("mb-field");
			m_gameField.AddToClassList("mb-grow");
			m_gameField.RegisterValueChangedCallback(OnGameChanged);
			m_vendorBar.Add(m_gameField);

			m_authLabel = new Label(string.Empty);
			m_authLabel.AddToClassList("mb-auth-state");
			m_vendorBar.Add(m_authLabel);

			m_authButton = new Button(OnAuthButtonClicked) { text = "Sign in" };
			m_vendorBar.Add(m_authButton);

			return m_vendorBar;
		}

		private static List<string> GetVendorDisplayNames()
		{
			return ModPublisherSession.AvailableVendors.Select(vendor => vendor.DisplayName).ToList();
		}

		private VisualElement BuildLeftPanel()
		{
			var left = new ScrollView(ScrollViewMode.Vertical);
			left.AddToClassList("mb-left-panel");

			left.Add(BuildPreviewBox());
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
				RefreshFormatAvailability();
			});
			m_buildSection.Add(m_formatField);

			m_formatBlockBox = new HelpBox(string.Empty, HelpBoxMessageType.Error);
			m_formatBlockBox.AddToClassList("mb-build-result-item");
			m_formatBlockBox.style.display = DisplayStyle.None;
			m_buildSection.Add(m_formatBlockBox);

			m_compressRow = new VisualElement();
			m_compressField = new EnumField("Compression", m_compressBuild);
			m_compressField.AddToClassList("mb-field");
			m_compressField.RegisterValueChangedCallback(evt => m_compressBuild = (CompressBuild)evt.newValue);
			m_compressRow.Add(m_compressField);
			m_buildSection.Add(m_compressRow);

			var actionsRow = new VisualElement();
			actionsRow.AddToClassList("mb-build-actions");

			m_validateButton = new Button(OnValidateButtonClicked)
			{
				text = "Validate",
				tooltip = "Check the map against every rule without building it. Nothing in the scene is modified.",
			};
			m_validateButton.AddToClassList("mb-build-button");
			actionsRow.Add(m_validateButton);

			m_buildButton = new Button(OnBuildButtonClicked) { text = "Build", tooltip = "Build the selected targets with the settings above" };
			m_buildButton.AddToClassList("mb-build-button");
			actionsRow.Add(m_buildButton);

			m_cancelButton = new Button(OnCancelButtonClicked) { text = "Cancel", tooltip = "Stop the operation in progress" };
			m_cancelButton.AddToClassList("mb-build-button");
			m_cancelButton.style.display = DisplayStyle.None;
			actionsRow.Add(m_cancelButton);

			m_buildSection.Add(actionsRow);

			m_buildStatus = new Label(string.Empty);
			m_buildStatus.AddToClassList("mb-publish-status");
			m_buildStatus.style.display = DisplayStyle.None;
			m_buildSection.Add(m_buildStatus);

			return m_buildSection;
		}

		private VisualElement BuildDestinationSection()
		{
			var box = new VisualElement();
			box.AddToClassList("mb-box");
			m_destinationSection = box;

			var header = new Label("Destination");
			header.AddToClassList("mb-section-header");
			box.Add(header);

			m_noItemHint = new HelpBox(
				"Select an item on the right to publish to, or create one with New Item once the build is done.",
				HelpBoxMessageType.Info);
			box.Add(m_noItemHint);

			// Choices are filled in by RefreshDestinationOptions once the active vendor is known.
			m_destinationGroup = new RadioButtonGroup(string.Empty, new List<string>());
			m_destinationGroup.AddToClassList("mb-destination-group");
			m_destinationGroup.RegisterValueChangedCallback(OnDestinationChanged);
			box.Add(m_destinationGroup);

			var publishHeader = new Label("Publish");
			publishHeader.AddToClassList("mb-section-header");
			box.Add(publishHeader);

			m_publishStatus = new Label(string.Empty);
			m_publishStatus.AddToClassList("mb-publish-status");
			m_publishStatus.style.display = DisplayStyle.None;
			box.Add(m_publishStatus);

			m_vendorPanel = new VisualElement();

			m_versionField = new TextField("Version")
			{
				tooltip = "Version label for this release, shown against the uploaded file. " +
				          "Left empty it falls back to the uploader version.",
			};
			m_versionField.AddToClassList("mb-field");
			m_versionField.RegisterValueChangedCallback(evt => WritePublishNotes(notes => notes.version = evt.newValue));
			m_vendorPanel.Add(m_versionField);

			m_changelogField = new TextField("Changelog")
			{
				multiline = true,
				tooltip = "What changed in this release. Shown to players on the mod page.",
			};
			m_changelogField.AddToClassList("mb-field");
			StretchToMultiline(m_changelogField, 90f);
			m_changelogField.RegisterValueChangedCallback(evt => WritePublishNotes(notes => notes.changelog = evt.newValue));
			m_vendorPanel.Add(m_changelogField);

			m_uploadNameToggle = new Toggle("Upload Name") { tooltip = "Overwrite the item title on upload" };
			m_uploadNameToggle.AddToClassList("mb-field");
			m_uploadNameToggle.RegisterValueChangedCallback(evt => MapManagerConfig.instance.uploadName = evt.newValue);
			m_vendorPanel.Add(m_uploadNameToggle);

			m_uploadDescriptionToggle = new Toggle("Upload Description") { tooltip = "Overwrite the item description on upload" };
			m_uploadDescriptionToggle.AddToClassList("mb-field");
			m_uploadDescriptionToggle.RegisterValueChangedCallback(evt => MapManagerConfig.instance.uploadDescription = evt.newValue);
			m_vendorPanel.Add(m_uploadDescriptionToggle);

			m_uploadPreviewToggle = new Toggle("Upload Preview") { tooltip = "Overwrite the item preview image on upload" };
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

			m_localButton = new Button(OnUploadLocalClicked) { text = "Update Local Test Copy", tooltip = "Copy this build into the item's local install folder, without publishing" };
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

			var header = new Label("Items");
			header.AddToClassList("mb-section-header");
			header.AddToClassList("mb-grow");
			headerRow.Add(header);

			var fetchButton = new Button(Fetch) { text = "Fetch", tooltip = "Reload the list of items from the vendor" };
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
				text = "New Item",
				tooltip = "Create a new item on the vendor and publish the finished build to it",
			};
			m_newItemButton.AddToClassList("mb-new-item-button");
			m_newItemButton.AddToClassList("mb-grow");
			actionsRow.Add(m_newItemButton);

			var deleteItemButton = new Button(OnDeleteItemClicked)
			{
				text = "Delete…",
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
			var vendors = ModPublisherSession.AvailableVendors;

			m_vendorField.choices = vendors.Select(vendor => vendor.DisplayName).ToList();

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
				ModAuthStatus.Authenticated => $"Signed in as {state.UserName}",
				ModAuthStatus.Authenticating => "Signing in…",
				ModAuthStatus.NotAuthenticated => "Not signed in",
				_ => "Unavailable",
			};

			// Vendors that inherit an ambient session (Steam) have nothing for a button to do.
			var interactive = auth?.RequiresInteractiveLogin ?? false;
			m_authButton.style.display = interactive ? DisplayStyle.Flex : DisplayStyle.None;
			m_authButton.text = state.IsAuthenticated ? "Sign out" : "Sign in";
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
						? "Sign in to the selected vendor to continue."
						: auth.State.Message;
			}

			m_unavailableBox.style.display = usable ? DisplayStyle.None : DisplayStyle.Flex;
			m_mainLayout.style.display = usable ? DisplayStyle.Flex : DisplayStyle.None;
		}


		private void RefreshDetailsPanel()
		{
			if (m_configField == null)
			{
				return;
			}

			var key = SelectKey;
			m_attaching.TryGetValue(key, out var isSelectAttach);

			MapManagerConfig.GetOrAttach(key, out var attachObj);

			// The config the panel works on: the one attached to the selected item, or the one picked by hand when
			// nothing is selected. Building only ever needs this - the vendor entry is a publishing concern.
			var activeConfig = attachObj?.metaConfig != null ? attachObj.metaConfig : m_pendingConfig;
			var buildData = MapManagerConfig.GetBuildOrEmpty(activeConfig);

			if (attachObj != null && m_buttonLastClickOnAnyItem)
			{
				m_compressBuild = buildData.compress;
				m_platformBuild = buildData.platform;
				m_buildType = buildData.buildSuccess;
				MapManagerConfig.instance.targetScene = buildData.targetScene;
				m_buttonLastClickOnAnyItem = false;
			}

			// Selecting an item adopts its config; with nothing selected the field keeps whatever was picked by hand,
			// which is what a brand new vendor account needs in order to create its first item at all.
			if (attachObj?.metaConfig != null)
			{
				m_pendingConfig = attachObj.metaConfig;
			}

			m_configField.SetValueWithoutNotify(attachObj != null ? attachObj.metaConfig : m_pendingConfig);

			RefreshPreview();
			RefreshDescription();
			RefreshNewItemHint();

			var hasConfig = activeConfig != null;
			m_buildAndPublishWrapper.style.display = hasConfig ? DisplayStyle.Flex : DisplayStyle.None;

			if (!hasConfig)
			{
				m_buildResultBox.Clear();
				return;
			}

			// Building is unlocked by the config alone. Requiring a vendor entry here would deadlock mod.io, where an
			// entry cannot be created without a payload to attach - and a payload is what building produces.
			var hasItem = key.IsValid && attachObj?.metaConfig != null && isSelectAttach;
			m_buildSection.SetEnabled(true);
			m_destinationSection.SetEnabled(true);

			m_buildTargetsField.SetValueWithoutNotify((TempData)m_buildType);
			m_formatField.SetValueWithoutNotify(m_buildFormat);
			m_compressField.SetValueWithoutNotify(m_compressBuild);
			UpdateCompressVisibility();

			RefreshSceneDropdown(buildData);

			var formatBlocked = RefreshFormatAvailability();

			m_buildButton.SetEnabled(m_buildType != 0 && !IsDownloadAnyIcon() && !m_buildProcess && !formatBlocked);
			m_validateButton.SetEnabled(!m_buildProcess && !IsDownloadAnyIcon());
			m_cancelButton.style.display = m_buildProcess ? DisplayStyle.Flex : DisplayStyle.None;

			var uploadState = RefreshBuildResult(activeConfig, buildData);

			RefreshDestinationOptions();
			MapManagerConfig.instance.buildLocal = m_publishDestination == PublishDestination.LocalTest;

			var notes = MapManagerConfig.GetPublishData(activeConfig);
			m_versionField.SetValueWithoutNotify(notes?.version ?? string.Empty);
			m_changelogField.SetValueWithoutNotify(notes?.changelog ?? string.Empty);

			// Steam has a change note but no version field, so asking for one there would be asking for nothing.
			var supportsVersion = MapBuilder.session.Limits?.SupportsVersion ?? false;
			m_versionField.style.display = supportsVersion ? DisplayStyle.Flex : DisplayStyle.None;

			m_uploadNameToggle.SetValueWithoutNotify(MapManagerConfig.instance.uploadName);
			m_uploadDescriptionToggle.SetValueWithoutNotify(MapManagerConfig.instance.uploadDescription);
			m_uploadPreviewToggle.SetValueWithoutNotify(MapManagerConfig.instance.uploadPreview);
			m_uploadVendorButton.text = $"Upload to {MapBuilder.session.Publisher?.DisplayName ?? "vendor"}";
			m_uploadVendorButton.SetEnabled(uploadState && hasItem);

			RefreshLocalPanel(uploadState);

			m_externalPathField.SetValueWithoutNotify(m_pathToExternal);
			m_externalExportButton.SetEnabled(!string.IsNullOrWhiteSpace(m_pathToExternal) && Path.IsPathFullyQualified(m_pathToExternal));

			UpdateDestinationPanels();
			var showNoItemHint = !hasItem && m_publishDestination == PublishDestination.Vendor;
			m_noItemHint.style.display = showNoItemHint ? DisplayStyle.Flex : DisplayStyle.None;
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
			m_localButton.SetEnabled(uploadState && resolved);
		}

		private void RefreshPreview()
		{
			// Falls back to the hand picked config so the preview is visible while no item exists yet.
			var config = MapManagerConfig.TryGetAttach(SelectKey, out var attachObj) && attachObj.metaConfig != null
				? attachObj.metaConfig
				: m_pendingConfig;

			var hasConfig = config != null;

			m_previewImage.style.display = hasConfig ? DisplayStyle.Flex : DisplayStyle.None;
			m_previewMissingBox.style.display = hasConfig ? DisplayStyle.None : DisplayStyle.Flex;

			if (hasConfig)
			{
				m_previewImage.image = config.mapMetaConfigValue.largeIcon;
			}

			m_previewIdLabel.text = SelectKey.IsValid ? SelectKey.id : string.Empty;
		}

		private void RefreshDescription()
		{
			var item = SelectItem;

			if (item == null)
			{
				m_descriptionText.text = string.Empty;
				return;
			}

			const int maxChars = 280;
			var description = item.Description;
			var isEmpty = string.IsNullOrWhiteSpace(description);
			var text = isEmpty ? "No description provided for this item." : description.Trim();

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

		private bool RefreshBuildResult(MapMetaConfig config, MapManagerConfig.BuildData buildData)
		{
			m_buildResultBox.Clear();

			if (config == null)
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
				else if (buildName == nameof(TempData.Meta) && !buildData.lastMeta.Equals(config.mapMetaConfigValue))
				{
					AddBuildResultBox($"Is Changed {buildName}! Please build {buildName}.", HelpBoxMessageType.Warning);
				}

				if (buildName == nameof(TempData.Map))
				{
					AddSceneStats(buildData.lastValid);
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

		private void AddSceneStats(ValidItemData stats)
		{
			var text = stats.ToString();

			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}

			var foldout = new Foldout { text = "Scene contents", value = false };
			foldout.AddToClassList("mb-build-result-item");

			var label = new Label(text) { enableRichText = false };
			label.AddToClassList("mb-muted");
			foldout.Add(label);

			m_buildResultBox.Add(foldout);
		}

		private void UpdateCompressVisibility()
		{
			m_compressRow.style.display = m_buildFormat != FormatBuild.dro2 ? DisplayStyle.Flex : DisplayStyle.None;
		}

		/// <summary>
		/// Shows why the selected format cannot be built by this editor, if it cannot.
		/// The format stays selectable on purpose: hiding dro1 would leave the author guessing where it went.
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
				PublishDestination.LocalTest => "Local Test",
				PublishDestination.ExternalFolder => "External Folder",
				_ => "Vendor",
			};
		}

		private void UpdateDestinationPanels()
		{
			m_vendorPanel.style.display = m_publishDestination == PublishDestination.Vendor ? DisplayStyle.Flex : DisplayStyle.None;
			m_localPanel.style.display = m_publishDestination == PublishDestination.LocalTest ? DisplayStyle.Flex : DisplayStyle.None;
			m_externalPanel.style.display = m_publishDestination == PublishDestination.ExternalFolder ? DisplayStyle.Flex : DisplayStyle.None;
		}


		private void OnConfigFieldChanged(ChangeEvent<UnityEngine.Object> evt)
		{
			var key = SelectKey;

			// Remembered even with nothing selected: on a vendor account with no items yet there is nothing to
			// attach to, and this is the config that seeds the first "New Item".
			m_pendingConfig = evt.newValue as MapMetaConfig;

			if (MapManagerConfig.GetOrAttach(key, out var attachObj) && attachObj != null)
			{
				// Attach writes the link through and saves the asset, so the choice survives a domain reload. Doing
				// it unconditionally also covers clearing the field, which detaches the item.
				MapManagerConfig.Attach(key, m_pendingConfig);
				m_attaching[key] = m_pendingConfig != null;
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
		}

		private async void OnBuildButtonClicked()
		{
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
			m_compressBuildCached = m_compressBuild;
			m_platformBuildCached = m_platformBuild;
			m_buildFormatCached = m_buildFormat;

			BeginOperation();
			RefreshDetailsPanel();

			try
			{
				await MapBuilder.BuildCustom((TempData)m_buildType,
					(TempData)buildData.buildSuccess,
					key,
					m_buildFormatCached,
					m_compressBuildCached,
					m_platformBuildCached,
					new Progress<string>(SetBuildStatus),
					m_operationCts.Token,
					(path, success) => AddBuild(config, buildData, path, success));
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
		}

		private void EndOperation()
		{
			m_operationCts?.Dispose();
			m_operationCts = null;

			m_buildProcess = false;
			SetBuildStatus(string.Empty);
			RefreshDetailsPanel();
		}

		private void SetBuildStatus(string message)
		{
			if (m_buildStatus == null)
			{
				return;
			}

			m_buildStatus.text = message ?? string.Empty;
			m_buildStatus.style.display = string.IsNullOrEmpty(m_buildStatus.text) ? DisplayStyle.None : DisplayStyle.Flex;
		}

		private void AddBuild(MapMetaConfig config,
			MapManagerConfig.BuildData buildData,
			string path,
			TempData complete)
		{
			m_loads[SelectKey] = false;

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

			RefreshDetailsPanel();
		}

	}
}
