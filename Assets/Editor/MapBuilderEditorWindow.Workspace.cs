using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public partial class MapBuilderEditorWindow
    {
        private const string LanguagePreference = "CarX.MapBuilder.Language";
        private static bool English;
        private static string L(string russian, string english) => English ? english : russian;

        private void ChangeLanguage(bool english)
        {
            if (m_buildProcess || English == english) return;
            var format = m_buildFormat;
            var targets = m_buildType;
            var scene = MapManagerConfig.instance.targetScene;
            var search = m_mapSearch.value;
            m_metadataFields.Unbind();
            EditorPrefs.SetString(LanguagePreference, english ? "en" : "ru");
            BuildWorkspace(rootVisualElement);
            m_buildFormat = format;
            m_buildType = targets;
            MapManagerConfig.instance.targetScene = scene;
            m_mapSearch.value = search;
            RefreshDetailsPanel();
        }
        [SerializeField] private int m_workspaceTab;
        private VisualElement m_localMapsPanel, m_mapPage, m_buildPage, m_publishPage, m_metadataForm;
        private VisualElement m_publications, m_metadataFields;
        private ProgressBar m_operationProgress;
        private Label m_formatHint;
        private VisualElement m_advancedPanel, m_sceneContentsPanel;
        private Label m_sceneContentsText;
        private Button m_advancedToggle, m_sceneContentsToggle;
        private int m_buildDetails;
        private ScrollView m_localMaps;
        private ToolbarSearchField m_mapSearch;
        private Label m_mapTitle, m_mapSubtitle, m_actionHint, m_buildSummary;
        private MapMetaConfig m_boundConfig;
        private Button[] m_tabs;
        private Button m_openBuildFolder;
        private readonly System.Diagnostics.Stopwatch m_operationTimer = new();
        private readonly Dictionary<MapMetaConfig, string> m_buildSummaries = new();
        private string m_lastOperationSummary = L("Выберите карту. Проверка и сборка доступны без входа в площадку.", "Select a map. Validation and building are available without signing in.");

        private void BuildWorkspace(VisualElement root)
        {
            English = EditorPrefs.GetString(LanguagePreference, "en") == "en";
            root.Clear();
            minSize = new Vector2(840, 580);
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (stylesheet != null && !root.styleSheets.Contains(stylesheet)) root.styleSheets.Add(stylesheet);
            root.AddToClassList("mb-root");
            root.RegisterCallback<GeometryChangedEvent>(evt => root.EnableInClassList("mb-compact", evt.newRect.width < 1120));
            var heading = new VisualElement(); heading.AddToClassList("mb-workspace-heading");
            var headerArt = new VisualElement { pickingMode = PickingMode.Ignore };
            headerArt.AddToClassList("mb-header-art");
            headerArt.style.backgroundImage = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Editor/MapBuilderHeader.png");
            heading.Add(headerArt);
            var shade = new VisualElement { pickingMode = PickingMode.Ignore };
            shade.AddToClassList("mb-header-shade"); heading.Add(shade);
            heading.Add(new Label("MapBuilder") { name = "workspace-title" });
            heading.Add(new Label(L("CarX Drift Racing Online 2  /  Создание карт", "CarX Drift Racing Online 2  /  Map creator")) { name = "workspace-caption" });
            var language = new DropdownField(new List<string> { "RU", "EN" }, English ? 1 : 0);
            language.name = "workspace-language";
            language.tooltip = L("Язык интерфейса", "Interface language");
            language.RegisterValueChangedCallback(evt => ChangeLanguage(evt.newValue == "EN"));
            language.SetEnabled(!m_buildProcess);
            heading.Add(language);
            root.Add(heading);
            m_mainLayout = new VisualElement(); m_mainLayout.AddToClassList("mb-main-layout"); root.Add(m_mainLayout);
            m_localMapsPanel = new VisualElement(); m_localMapsPanel.AddToClassList("mb-library");
            m_localMapsPanel.Add(new Label(L("ЛОКАЛЬНЫЕ КАРТЫ", "LOCAL MAPS")) { name = "library-title" });
            m_mapSearch = new ToolbarSearchField(); m_mapSearch.RegisterValueChangedCallback(_ => RefreshLocalMaps());
            m_localMapsPanel.Add(m_mapSearch);
            var add = new Button(CreateLocalMap) { text = L("+  Новая карта", "+  New map"), tooltip = L("Создать новую карту и файл её настроек.", "Create a new map and its settings asset.") }; add.AddToClassList("mb-library-add"); m_localMapsPanel.Add(add);
            m_localMaps = new ScrollView(); m_localMaps.AddToClassList("mb-library-list"); m_localMapsPanel.Add(m_localMaps);
            m_mainLayout.Add(m_localMapsPanel);

            var workspace = new VisualElement(); workspace.AddToClassList("mb-workspace"); m_mainLayout.Add(workspace);
            m_mapTitle = new Label(); m_mapTitle.AddToClassList("mb-map-title"); workspace.Add(m_mapTitle);
            m_mapSubtitle = new Label(); m_mapSubtitle.AddToClassList("mb-muted"); workspace.Add(m_mapSubtitle);
            var tabBar = new VisualElement(); tabBar.AddToClassList("mb-tabs"); workspace.Add(tabBar);
            m_tabs = new Button[3];
            var names = new[] { L("Карта", "Map"), L("Сборка", "Build"), L("Публикация", "Publish") };
            for (int i = 0; i < names.Length; i++)
            {
                int tab = i; m_tabs[i] = new Button(() => SelectWorkspaceTab(tab)) { text = names[i] };
                m_tabs[i].AddToClassList("mb-tab"); tabBar.Add(m_tabs[i]);
            }
            m_buildAndPublishWrapper = new VisualElement(); m_buildAndPublishWrapper.AddToClassList("mb-pages"); workspace.Add(m_buildAndPublishWrapper);
            m_mapPage = new ScrollView(); m_mapPage.AddToClassList("mb-page"); m_buildAndPublishWrapper.Add(m_mapPage);
            m_mapSearch.style.width = 220; m_mapSearch.style.maxWidth = 220;
            m_metadataForm = new VisualElement(); m_metadataForm.AddToClassList("mb-map-form"); m_mapPage.Add(m_metadataForm);
            BuildPreviewBox(); // Retains publication preview state; editing uses the image pickers below.
            m_metadataFields = new VisualElement(); m_metadataFields.AddToClassList("mb-meta-fields"); m_metadataForm.Add(m_metadataFields);
            m_metadataFields.RegisterCallback<SerializedPropertyChangeEvent>(OnMapMetadataChanged);

            m_buildPage = new ScrollView(); m_buildPage.AddToClassList("mb-page"); m_buildAndPublishWrapper.Add(m_buildPage);
            m_buildPage.Add(BuildBuildSection());
            var buildHeader = new VisualElement(); buildHeader.AddToClassList("mb-build-header");
            buildHeader.Add(m_buildSection.ElementAt(0)); m_buildSection.Insert(0, buildHeader);
            var detailButtons = new VisualElement(); detailButtons.AddToClassList("mb-build-detail-buttons"); buildHeader.Add(detailButtons);
            m_advancedToggle = new Button(() => ToggleBuildDetails(1)) { text = L("Дополнительные настройки", "Advanced settings") };
            m_sceneContentsToggle = new Button(() => ToggleBuildDetails(2)) { text = L("Состав сцены", "Scene contents") };
            detailButtons.Add(m_advancedToggle); detailButtons.Add(m_sceneContentsToggle);
            m_advancedPanel = new VisualElement(); m_advancedPanel.AddToClassList("mb-build-detail-panel");
            m_advancedPanel.Add(m_buildTargetsField); m_advancedPanel.Add(m_binaryTexturesField);
            m_buildSection.Insert(1, m_advancedPanel);
            m_sceneContentsPanel = new VisualElement(); m_sceneContentsPanel.AddToClassList("mb-build-detail-panel");
            m_sceneContentsText = new Label { enableRichText = false }; m_sceneContentsText.style.whiteSpace = WhiteSpace.Normal;
            m_sceneContentsPanel.Add(m_sceneContentsText); m_buildSection.Insert(2, m_sceneContentsPanel);
            RefreshBuildDetails();
            m_buildButton.AddToClassList("mb-primary");
            m_buildSummary = new Label(); m_buildSummary.AddToClassList("mb-build-summary"); m_buildPage.Add(m_buildSummary);
            m_buildResultBox = new VisualElement(); m_buildPage.Add(m_buildResultBox);
            var buildActions = new VisualElement(); buildActions.AddToClassList("mb-row");
            m_openBuildFolder = new Button(() =>
            {
                var path = MapManagerConfig.GetBuildOrEmpty(m_pendingConfig).path;
                if (Directory.Exists(path)) EditorUtility.RevealInFinder(path);
            }) { text = L("Открыть папку сборки", "Open build folder") };
            buildActions.Add(m_openBuildFolder);
            buildActions.Add(new Button(() => SelectWorkspaceTab(2)) { text = L("Перейти к публикации →", "Go to publishing →") }); m_buildPage.Add(buildActions);

            m_publishPage = new ScrollView(); m_publishPage.AddToClassList("mb-page"); m_buildAndPublishWrapper.Add(m_publishPage);
            BuildVendorBar();
            var account = new VisualElement { name = "workspace-account" };
            account.Add(m_authLabel); account.Add(m_authButton); heading.Add(account);
            m_vendorField.name = "workspace-platform";
            m_vendorField.choices = new List<string> { "mod.io" };
            m_vendorField.SetValueWithoutNotify("mod.io");
            m_vendorField.SetEnabled(false);
            heading.Insert(heading.IndexOf(language), m_vendorField);
            m_unavailableBox = new VisualElement(); m_unavailableHelp = new HelpBox("", HelpBoxMessageType.Info);
            m_unavailableBox.Add(m_unavailableHelp); m_unavailableBox.style.display = DisplayStyle.None; m_publishPage.Add(m_unavailableBox);
            m_publishPage.Add(BuildDestinationSection());
            m_publications = BuildRightPanel(); m_publications.AddToClassList("mb-publications"); m_publishPage.Add(m_publications);
            var hints = new VisualElement(); hints.AddToClassList("mb-workspace-hints"); workspace.Add(hints);
            m_actionHint = new Label(); m_actionHint.AddToClassList("mb-action-hint"); hints.Add(m_actionHint);
            m_formatHint = new Label(L("Binary — компактный контейнер карты. Проверка найдёт ошибки до сборки.", "Binary is a compact map container. Validate the map to find issues before building."));
            m_formatHint.AddToClassList("mb-format-hint"); hints.Add(m_formatHint);
            var footer = new VisualElement(); footer.AddToClassList("mb-operation-footer"); root.Add(footer);
            footer.Add(m_buildStatus);
            m_operationProgress = new ProgressBar { lowValue = 0, highValue = 100, title = "0%" };
            m_operationProgress.AddToClassList("mb-operation-progress");
            m_operationProgress.style.display = m_buildProcess ? DisplayStyle.Flex : DisplayStyle.None;
            footer.Add(m_operationProgress); footer.Add(m_cancelButton);
            m_buildStatus.AddToClassList("mb-grow");
            if (m_pendingConfig == null) m_pendingConfig = MapManagerConfig.instance.mapMetaConfigValue;
            if (m_pendingConfig == null) m_pendingConfig = FindLocalMaps().FirstOrDefault();
            SelectLocalMap(m_pendingConfig);
            SelectWorkspaceTab(m_workspaceTab);
            RefreshVendorBar(); RefreshAvailability(); RefreshItemsList();
            SetBuildStatus(string.Empty);
        }

        private IEnumerable<MapMetaConfig> FindLocalMaps() => AssetDatabase.FindAssets("t:MapMetaConfig")
            .Select(guid => AssetDatabase.LoadAssetAtPath<MapMetaConfig>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(config => config != null).OrderBy(MapDisplayName);

        private static string MapDisplayName(MapMetaConfig config) => config == null ? L("Выберите карту", "Select a map") :
            string.IsNullOrWhiteSpace(config.mapMetaConfigValue.mapName) ? config.name : config.mapMetaConfigValue.mapName;

        private static bool IsBuildCurrent(MapMetaConfig config, MapManagerConfig.BuildData build) =>
            config != null && build.buildSuccess == 3 && !string.IsNullOrEmpty(build.path) &&
            Directory.Exists(Path.Combine(build.path, "MapTemp")) && Directory.Exists(Path.Combine(build.path, "MetaTemp")) &&
            !MapBuilder.IsFormatBlocked(build.format, out _) &&
            build.lastMeta.Equals(config.mapMetaConfigValue);

        private void RefreshLocalMaps()
        {
            if (m_localMaps == null) return;
            m_localMaps.Clear();
            foreach (var config in FindLocalMaps())
            {
                if (!string.IsNullOrWhiteSpace(m_mapSearch.value) && MapDisplayName(config).IndexOf(m_mapSearch.value, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var build = MapManagerConfig.GetBuildOrEmpty(config);
                var row = new Button(() => SelectLocalMap(config)); row.AddToClassList("mb-map-row");
                row.EnableInClassList("mb-map-selected", config == m_pendingConfig);
                row.Add(new Image { image = config.mapMetaConfigValue.icon ?? config.mapMetaConfigValue.largeIcon, scaleMode = ScaleMode.ScaleToFit });
                var info = new VisualElement(); info.AddToClassList("mb-map-row-info"); row.Add(info);
                info.Add(new Label(MapDisplayName(config)) { tooltip = AssetDatabase.GetAssetPath(config) });
                var status = new Label(IsBuildCurrent(config, build) ? L("Готова к экспорту", "Ready to export") : build.buildSuccess == 0 ? L("Не собрана", "Not built") : L("Нужна пересборка", "Rebuild needed"));
                status.AddToClassList("mb-muted"); info.Add(status); m_localMaps.Add(row);
                var remote = new Label(PublishedVersionLabel(config));
                remote.AddToClassList("mb-muted"); remote.tooltip = remote.text; info.Add(remote);
            }
            if (m_localMaps.childCount == 0) m_localMaps.Add(new Label(L("Карт не найдено.\nДобавьте карту или измените поиск.", "No maps found.\nAdd a map or change your search.")) { style = { whiteSpace = WhiteSpace.Normal } });
        }

        private string PublishedVersionLabel(MapMetaConfig config)
        {
            var link = MapManagerConfig.instance.attachingConfigs.FirstOrDefault(item => item != null && item.metaConfig == config && item.key.vendor == "modio");
            if (link == null) return L("mod.io: нет связанной публикации", "mod.io: not linked");
            var published = m_fetchResultListItems.FirstOrDefault(item => item.Key == link.key);
            if (published == null || !MapBuilder.session.IsAuthenticated) return L("mod.io: версия не загружена", "mod.io: version not loaded");
            if (published.PayloadSizeBytes <= 0) return L("mod.io: нет активного файла", "mod.io: no active file");
            return "mod.io: " + (string.IsNullOrWhiteSpace(published.PublishedVersion) ? L("версия не указана", "version not set") : published.PublishedVersion);
        }

        private void SelectLocalMap(MapMetaConfig config)
        {
            if (m_buildProcess) return;
            ((ScrollView)m_mapPage).scrollOffset = Vector2.zero;
            m_lastOperationSummary = config == null ? L("Добавьте карту или выберите настройки существующей.", "Add a map or select an existing map config.") : L("Локальная карта: ", "Local map: ") + MapDisplayName(config);
            m_pendingConfig = config; m_selectItemIndex = -1; m_buttonLastClickOnAnyItem = false;
            for (int i = 0; i < m_fetchResultListItems.Count; i++)
                if (MapManagerConfig.TryGetAttach(m_fetchResultListItems[i].Key, out var link) && link.metaConfig == config && config != null) { m_selectItemIndex = i; break; }
            var build = MapManagerConfig.GetBuildOrEmpty(config);
            m_buildType = 3;
            m_buildFormat = MapBuilder.IsFormatBlocked(build.format, out _) ? FormatBuild.Binary : build.format;
            MapManagerConfig.instance.mapMetaConfigValue = config;
            if (!string.IsNullOrEmpty(build.targetScene)) MapManagerConfig.instance.targetScene = build.targetScene;
            else MapManagerConfig.instance.targetScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            RefreshDetailsPanel(); RefreshLocalMaps(); RefreshItemsList(); SetBuildStatus(string.Empty);
        }

        private void CreateLocalMap()
        {
            var path = EditorUtility.SaveFilePanelInProject(L("Новая карта", "New map"), "NewMapConfig", "asset", L("Где сохранить настройки карты?", "Where should the map settings be saved?"));
            if (string.IsNullOrEmpty(path)) return;
            var config = CreateInstance<MapMetaConfig>(); config.id = Guid.NewGuid().ToString("N");
            config.mapMetaConfigValue.mapName = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(config, path); AssetDatabase.SaveAssets(); SelectLocalMap(config); SelectWorkspaceTab(0);
        }

        private void RefreshWorkspace(MapMetaConfig config, MapManagerConfig.BuildData build)
        {
            m_mapTitle.text = MapDisplayName(config);
            m_mapSubtitle.text = config == null ? L("Создайте карту или выберите настройки существующей.", "Create a map or select an existing map config.") :
                $"{(string.IsNullOrEmpty(build.targetScene) ? L("Сцена выбирается во вкладке «Сборка»", "Choose a scene in the Build tab") : Path.GetFileNameWithoutExtension(build.targetScene))}  ·  {m_buildFormat}";
            if (config != null && !m_buildSummaries.ContainsKey(config) && EditorPrefs.HasKey("CarX.MapBuilder.BuildSummary." + config.id))
                m_buildSummaries[config] = EditorPrefs.GetString("CarX.MapBuilder.BuildSummary." + config.id);
            m_buildSummary.text = config != null && m_buildSummaries.TryGetValue(config, out var summary) ? summary : string.Empty;
            m_buildSummary.style.display = string.IsNullOrEmpty(m_buildSummary.text) ? DisplayStyle.None : DisplayStyle.Flex;
            UpdateTabHints();
            m_openBuildFolder.SetEnabled(config != null && Directory.Exists(build.path));
            if (m_boundConfig != config || m_metadataFields.childCount == 0)
            {
                m_metadataFields.Unbind(); m_metadataFields.Clear(); m_boundConfig = config;
                if (config != null)
                {
                    BuildMetadataEditor(config);
                    m_metadataFields.Bind(new SerializedObject(config));
                }
            }
        }

        private void BuildMetadataEditor(MapMetaConfig config)
        {
            var identity = new VisualElement(); identity.AddToClassList("mb-meta-identity"); m_metadataFields.Add(identity);
            var title = new TextField(L("Название", "Name"))
                { bindingPath = "mapMetaConfigValue.mapName" };
            title.AddToClassList("mb-meta-stacked"); title.AddToClassList("mb-meta-name"); identity.Add(title);
            var version = new TextField(L("Версия", "Version"))
                { bindingPath = "mapMetaConfigValue.mapVersion", tooltip = L("Например, 1.0.0. Если пусто — 1.0.0.", "For example, 1.0.0. Defaults to 1.0.0 when empty.") };
            version.AddToClassList("mb-meta-stacked"); version.AddToClassList("mb-meta-version"); identity.Add(version);
            var summary = new TextField(L("Краткое описание (необязательно)", "Summary (optional)"))
                { bindingPath = "mapMetaConfigValue.summary", tooltip = L("Если пусто, используется первая строка описания.", "When empty, the first line of the description is used.") };
            summary.AddToClassList("mb-meta-stacked"); m_metadataFields.Add(summary);
            var columns = new VisualElement(); columns.AddToClassList("mb-meta-columns"); m_metadataFields.Add(columns);
            var description = new TextField(L("Описание", "Description"))
                { bindingPath = "mapMetaConfigValue.mapDescription", multiline = true, verticalScrollerVisibility = ScrollerVisibility.Auto };
            description.AddToClassList("mb-meta-description"); description.AddToClassList("mb-meta-stacked"); columns.Add(description);
            var images = new VisualElement(); images.AddToClassList("mb-meta-images"); columns.Add(images);
            AddMetadataImage(images, config, "largeIcon", L("Превью · 16:9", "Preview · 16:9"), 150);
            AddMetadataImage(images, config, "icon", L("Иконка · 16:9", "Icon · 16:9"), 104);
            var extra = new VisualElement();
            extra.Add(new PropertyField { bindingPath = "mapMetaConfigValue.authors", label = L("Авторы", "Authors") });
            extra.Add(new PropertyField { bindingPath = "mapMetaConfigValue.url", label = L("Ссылка", "URL") });
            m_metadataFields.Add(extra);
        }

        private void AddMetadataImage(VisualElement parent, MapMetaConfig config, string propertyName, string label, float width)
        {
            parent.Add(new Label(label));
            var picker = new IMGUIContainer(() =>
            {
                if (config == null) return;
                using var serialized = new SerializedObject(config);
                var property = serialized.FindProperty("mapMetaConfigValue." + propertyName);
                var rect = GUILayoutUtility.GetRect(width, width * 9f / 16f, GUILayout.MaxWidth(width));
                EditorGUI.BeginChangeCheck();
                var texture = EditorGUI.ObjectField(rect, property.objectReferenceValue, typeof(Texture2D), false);
                if (EditorGUI.EndChangeCheck())
                {
                    property.objectReferenceValue = texture;
                    serialized.ApplyModifiedProperties();
                    AssetDatabase.SaveAssetIfDirty(config);
                    m_metadataFields.schedule.Execute(() => { RefreshDetailsPanel(); RefreshLocalMaps(); });
                }
            });
            picker.style.height = width * 9f / 16f + 8;
            parent.Add(picker);
        }

        private void OnMapMetadataChanged(SerializedPropertyChangeEvent evt)
        {
            if (m_pendingConfig != null) AssetDatabase.SaveAssetIfDirty(m_pendingConfig);
            RefreshDetailsPanel(); RefreshLocalMaps();
        }

        private void SelectWorkspaceTab(int tab)
        {
            if (tab == 0 && m_workspaceTab != 0) ((ScrollView)m_mapPage).scrollOffset = Vector2.zero;
            m_workspaceTab = Mathf.Clamp(tab, 0, 2);
            var pages = new[] { m_mapPage, m_buildPage, m_publishPage };
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].style.display = i == m_workspaceTab ? DisplayStyle.Flex : DisplayStyle.None;
                m_tabs[i].EnableInClassList("mb-tab-selected", i == m_workspaceTab);
            }
            UpdateTabHints();
        }

        private void UpdateTabHints()
        {
            var inBuild = m_workspaceTab == 1 && m_pendingConfig != null;
            m_actionHint.style.display = m_workspaceTab == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            m_buildAndPublishWrapper.style.display = m_pendingConfig != null || m_workspaceTab == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            m_formatHint.style.display = inBuild && m_buildFormat == FormatBuild.Binary ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ToggleBuildDetails(int panel)
        {
            m_buildDetails = m_buildDetails == panel ? 0 : panel;
            RefreshBuildDetails();
        }

        private void RefreshBuildDetails()
        {
            m_advancedPanel.style.display = m_buildDetails == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            m_sceneContentsPanel.style.display = m_buildDetails == 2 ? DisplayStyle.Flex : DisplayStyle.None;
            m_advancedToggle.EnableInClassList("mb-detail-selected", m_buildDetails == 1);
            m_sceneContentsToggle.EnableInClassList("mb-detail-selected", m_buildDetails == 2);
        }

        private bool IsSelectedBuildReady()
        {
            var build = MapManagerConfig.GetBuildOrEmpty(m_pendingConfig);
            return IsBuildCurrent(m_pendingConfig, build) && build.format == m_buildFormat && build.targetScene == MapManagerConfig.instance.targetScene;
        }

        private bool CanExportCurrentMap() => !m_buildProcess && IsSelectedBuildReady() &&
            !string.IsNullOrWhiteSpace(m_pathToExternal) && Path.IsPathFullyQualified(m_pathToExternal);

        private void RefreshExportButton() => m_externalExportButton?.SetEnabled(CanExportCurrentMap());

        private void UpdateWorkspaceReadiness(bool ready)
        {
            m_actionHint.text = ready ? L("Сборка готова к отправке.", "Build ready to upload.") : L("Для экспорта сначала соберите карту и её данные во вкладке «Сборка».", "Before exporting, build the map and its metadata in the Build tab.");
            if (ready && m_publishDestination == PublishDestination.ExternalFolder && string.IsNullOrWhiteSpace(m_pathToExternal))
                m_actionHint.text = L("Выберите папку для экспорта.", "Choose an export folder.");
            m_newItemButton.SetEnabled(ready && !m_buildProcess && MapBuilder.session.IsReady && MapBuilder.session.IsAuthenticated);
            rootVisualElement.Q<DropdownField>("workspace-language")?.SetEnabled(!m_buildProcess);
            rootVisualElement.Q<VisualElement>("workspace-account")?.SetEnabled(!m_buildProcess);
            m_publications.SetEnabled(!m_buildProcess);
            m_publications.style.display = m_publishDestination == PublishDestination.Vendor ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshExportButton();
        }

        private void RecordBuildResult(MapMetaConfig config, string path, TempData complete)
        {
            m_operationTimer.Stop();
            long bytes = Directory.Exists(path) ? Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length) : 0;
            m_lastOperationSummary = complete == (TempData.Map | TempData.Meta)
                ? string.Format(L("Собрано за {0:F1} с · Кэш ресурсов: {1:F1} МБ", "Built in {0:F1} s · Resource cache: {1:F1} MB"), m_operationTimer.Elapsed.TotalSeconds, bytes / 1048576d)
                : L("Сборка не завершена полностью. Проверьте результаты проверки и Console.", "Build incomplete. Check validation results and the Console.");
            m_buildSummaries[config] = m_lastOperationSummary;
            EditorPrefs.SetString("CarX.MapBuilder.BuildSummary." + config.id, DateTime.Now.ToString("dd.MM.yyyy HH:mm") + " · " + m_lastOperationSummary);
            RefreshLocalMaps();
        }

        private void OnProjectChange() { if (m_localMaps != null && !m_buildProcess) RefreshLocalMaps(); }

        private sealed class ImmediateProgress<T> : IProgress<T>
        {
            private readonly Action<T> report;
            public ImmediateProgress(Action<T> report) { this.report = report; }
            public void Report(T value) => report(value);
        }

        private static string LocalizeOperation(string message) => message switch
        {
            "Checking the map configuration…" => L("Проверка настроек карты…", "Checking map settings…"),
            "Validating and collecting the scene…" => L("Проверка и сбор ресурсов сцены…", "Validating and collecting scene resources…"),
            "Building the map…" => L("Сборка геометрии карты…", "Building map geometry…"),
            "Building the metadata…" => L("Сборка данных карты…", "Building map metadata…"),
            "Creating the item…" => L("Создание публикации…", "Creating publication…"),
            "Connecting to mod.io…" => L("Подключение к mod.io…", "Connecting to mod.io…"),
            "Preparing publication files…" => L("Подготовка файлов публикации…", "Preparing publication files…"),
            "Sending files / waiting for mod.io…" => L("Отправка файлов / ожидание mod.io…", "Sending files / waiting for mod.io…"),
            "Cancelling…" => L("Отмена операции…", "Cancelling…"),
            "Uploading…" => L("Отправка файлов…", "Uploading files…"),
            _ => message
        };
    }
}
