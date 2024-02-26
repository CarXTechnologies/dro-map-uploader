using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameOverlay;
using Steamworks.Ugc;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace Editor
{
    public class MapBuilderEditorWindow : EditorWindow
    {
        private Vector2 m_scrollPosition;
        private Vector2 m_scrollPositionPreview;
        private Task m_fetchTask;
        private SteamUGCManager m_steamUgc;
        private int m_selectItemIndex;
        private readonly List<Item> m_fetchResultListItems = new List<Item>();

        private Item m_selectItem => m_selectItemIndex >= 0 && m_selectItemIndex < m_fetchResultListItems.Count
            ? m_fetchResultListItems[m_selectItemIndex]
            : default;
        
        private readonly Dictionary<ulong, bool> m_loads = new Dictionary<ulong, bool>();
        private readonly Dictionary<ulong, bool> m_attahing = new Dictionary<ulong, bool>();
        private readonly Dictionary<ulong, (Texture2D, bool)> images = new Dictionary<ulong, (Texture2D, bool)>();

        private readonly Queue<Action> m_queueActionDraw = new Queue<Action>();
        
        private Property m_configProperty;
        private int m_buildType;
        private PlatformBuild m_platformBuild;
        private CompressBuild m_compressBuild;

        private string[] m_iconLoad =
        {
            "d_WaitSpin00", "d_WaitSpin01", "d_WaitSpin02", "d_WaitSpin03", "d_WaitSpin04", "d_WaitSpin05",
            "d_WaitSpin06", "d_WaitSpin07", "d_WaitSpin08", "d_WaitSpin09", "d_WaitSpin10", "d_WaitSpin11"
        };

        private void OnEnable()
        {
            Clear();
            Fetch();
        }

        [MenuItem("Tools/MapBuilder")]
        public static void ShowMyEditor()
        {
            MapBuilderEditorWindow wnd = GetWindow<MapBuilderEditorWindow>();
            wnd.titleContent = new GUIContent("MapBuilder");
            wnd.Fetch();
        }

        private async void Fetch()
        {
            MapBuilder.InitSteamUGC();
            m_steamUgc = MapBuilder.steamUgc;
            await FetchItems();
        }

        private void OnDisable()
        {
            Clear();
        }

        public void Clear()
        {
            foreach (var image in images)
            {
                DestroyImmediate(image.Value.Item1);
            }
        }

        private bool m_buildProcess;
        
        private async Task FetchItems()
        {
            while (m_buildProcess)
            {
                await Task.Delay(100);
            }
            
            await m_steamUgc.GetWorkshopItems(m_fetchResultListItems, DownloadSpriteAsync);
            
            foreach (var item in m_fetchResultListItems)
            {
                m_attahing[item.Id] = MapManagerConfig.IsAttach(item.Id);
            }
            
            MapManagerConfig.ValidBuildsAndAttaching(m_fetchResultListItems);
        }
        
        private async void DownloadSpriteAsync(Item item)
        {
            if (images.TryGetValue(item.Id, out var image) && image.Item2)
            {
                return;
            }

            while (m_buildProcess)
            {
                await Task.Delay(100);
            }
            
            if (image.Item1 != null)
            {
                DestroyImmediate(image.Item1);
            }

            images[item.Id] = (null, true);
            await UIUtils.DownloadSprite(item.PreviewImageUrl, (sprite, texture2D) =>
            {
                images[item.Id] = (texture2D == null ? new Texture2D(1, 1) : texture2D, false);
            });
        }
        
        [Obsolete("Obsolete")]
        private void OnGUI()
        {
            GUI.skin.box.normal.textColor = Color.white;
            const float aspect = 16.0f / 9.0f;
            const float sizeImage = 200;
            const float sizeButton = 18;
            var rectPreview = new Rect(16, 28, sizeImage * aspect, sizeImage);
            var rectCenter = new Rect((sizeImage * aspect) / 2f - 64, sizeImage / 2f, 164f, 32f);
            var rectItem = new Rect(rectPreview.width + rectPreview.x + 32, 0, position.width - 48 - (rectPreview.width + rectPreview.x + 16), 80);

            const float space = 6;
            float elementHeight = space + rectItem.height;
            var iconSteam = EditorGUIUtility.IconContent("steam");
            bool uploadState = true;
            
            m_scrollPosition = GUI.BeginScrollView(
                new Rect(rectItem.x + space * 2, 0, rectItem.width + space * 2, position.height), m_scrollPosition, 
                new Rect(rectItem.x - 2, 0, rectItem.width, elementHeight * (m_fetchResultListItems.Count + 1) + space));
            
            m_attahing.TryGetValue(m_selectItem.Id, out var isSelectAttach);
            
            MapManagerConfig.BuildData buildData = default;
            
            MapManagerConfig.GetOrAttach(m_selectItem.Id, out var attachObj);
            
            if (attachObj != null)
            {
                buildData = MapManagerConfig.GetBuildOrEmpty(attachObj.metaConfig);
            }

            var loadIcon = EditorGUIUtility.IconContent(m_iconLoad[Mathf.FloorToInt((Time.time * 12) % m_iconLoad.Length)]);
            for (int i = 0; i < m_fetchResultListItems.Count; i++)
            {
                rectItem.y += space;
                var item = m_fetchResultListItems[i];
                GUI.Box(rectItem, string.Empty);
                if (GUI.Button(rectItem, string.Empty))
                {
                    if (i != m_selectItemIndex && !string.IsNullOrWhiteSpace(buildData.targetScene))
                    {
                        MapManagerConfig.instance.targetScene = buildData.targetScene;
                    }
                    m_selectItemIndex = i;
                }
                rectItem.x += rectItem.height + space;
                rectItem.height /= 2;
                var oldSize = GUI.skin.label.fontSize;
                GUI.skin.label.fontSize = 16;
                GUI.Label(rectItem, string.IsNullOrWhiteSpace(item.Title)? $"Blank {i}" : item.Title);
                GUI.skin.label.fontSize = oldSize;
                rectItem.y += rectItem.height;
                SteamUGCManager.GetItemDetail(item);
                GUI.Label(rectItem, 
                    $"{Mathf.FloorToInt(SteamUGCManager.GetItemDetail(item).FileSize / ModMapTestTool.BYTES_TO_MEGABYTES)} / " +
                    $"{(ModMapTestTool.Steam.maxSizeInMb + ModMapTestTool.Steam.maxSizeInMbMeta)} mb");
                rectItem.y -= rectItem.height;
                rectItem.height *= 2;
                rectItem.x -= rectItem.height + space;
                
                var rectItemWarning = new Rect(rectItem.x + rectItem.width - 48, rectItem.y, 48, 18);
                
                if (m_selectItemIndex == i)
                {
                    GUI.color = Color.black;
                    GUI.Box(rectItem, String.Empty);
                    GUI.color = Color.white;
                }

                if (!MapManagerConfig.TryGetAttach(m_fetchResultListItems[i].Id, out var attachData) || 
                    attachData.metaConfig == null)
                {
                    GUI.color = Color.red;
                    GUI.Box(rectItemWarning, "Detach");
                    GUI.color = Color.white;
                }
                
                rectItem.x += space;

                var rectItemPreview = rectItem;
                rectItemPreview.width = 128 * (9f / 16f);

                if (images.TryGetValue(item.Id, out var imageData) && !imageData.Item2)
                {
                    if(imageData.Item1 != null && imageData.Item1.width > 1)
                    {
                        rectItemPreview.y += space;
                        rectItemPreview.height -= space * 2;
                        EditorGUI.DrawRect(rectItemPreview, Color.black);
                        rectItemPreview.y -= space;
                        rectItemPreview.height = rectItemPreview.width * ((float)imageData.Item1.height / imageData.Item1.width);
                        rectItemPreview.y += rectItem.height / 2 - rectItemPreview.height / 2;
                        GUI.DrawTexture(rectItemPreview, imageData.Item1);
                    }
                    else
                    {
                        rectItemPreview.y += space;
                        rectItemPreview.height -= space * 2;
                        GUI.Box(rectItemPreview, String.Empty);
                        rectItemPreview.x += rectItemPreview.width / 4;
                        GUI.Label(rectItemPreview, "Empty");
                        rectItemPreview.x -= rectItemPreview.width / 4;
                    }

                    if (imageData.Item1 == null)
                    {
                        DownloadSpriteAsync(item);
                    }
                }
                else if (m_loads.TryGetValue(item.Id, out var state) && state)
                {
                    GUI.Label(rectItemPreview, loadIcon);
                }
                
                rectItem.x -= space;
                rectItem.y += rectItem.height;
            }

            rectItem.y += space;
            rectItem.height = 40;
            if (GUI.Button(rectItem, "New Workshop Item"))
            {
                MapBuilder.CreateNewCommunityFile(result =>
                {
                    if (result.Success)
                    {
                        Fetch();
                    }
                });
            };
            GUI.Label(rectItem, iconSteam);
            
            GUI.EndScrollView();
            
            Rect lastRect;
            
            var rectConfig = new Rect(
                rectPreview.x, rectPreview.y + rectPreview.height + space,
                rectPreview.width, sizeButton);

            var rectConfigValue = new Rect(
                rectConfig.x, rectConfig.y + rectConfig.height,
                rectConfig.width, sizeButton);
            
            var rectButtons = new Rect(
                rectConfigValue.x, rectConfigValue.y + space * 2,
                rectConfigValue.width, sizeButton);
            
            var rectBuildSettings = new Rect(
                rectPreview.x, rectButtons.y - space,
                rectPreview.width, sizeButton + space / 2);
            
            var rectPlatform = new Rect(
                rectBuildSettings.x + rectBuildSettings.width / 2, rectBuildSettings.y+ sizeButton + space * 2,
                rectBuildSettings.width / 2, sizeButton);
            
            var rectScene = new Rect(
                rectPlatform.x, rectPlatform.y + sizeButton + space,
                rectPlatform.width, sizeButton);

            //var rectCompress = new Rect(
            //    rectScene.x, rectScene.y + sizeButton + space,
            //   rectScene.width, sizeButton);

            var rectPlatformName = new Rect(
                rectBuildSettings.x, rectPlatform.y,
                rectBuildSettings.width / 2, sizeButton);
            
            //var rectCompressName = new Rect(
            //    rectPlatformName.x, rectCompress.y,
            //    rectPlatformName.width, sizeButton);
            
            var rectSceneName = new Rect(
                rectPlatformName.x, rectScene.y,
                rectPlatformName.width, sizeButton);
            
            var rectSplitRight = new Rect(
                rectSceneName.x + rectSceneName.width, rectSceneName.y + sizeButton + space,
                rectSceneName.width, sizeButton);

            var rectSplitLeft = new Rect(
                rectSceneName.x, rectSceneName.y + sizeButton + space,
                rectSceneName.width, sizeButton);
            
            lastRect = rectSplitLeft;
            
            var rectSplitBuild = new Rect(
                rectSceneName.x, lastRect.y + sizeButton + space,
                rectBuildSettings.width, sizeButton * 1.5f);

            var rectUploadSettings = new Rect(
                rectBuildSettings.x, rectSplitBuild.y + sizeButton + space * 3,
                rectBuildSettings.width, sizeButton + space / 2);
            
            var rectUploadSteamNameToggle = new Rect(
                rectUploadSettings.x + rectSceneName.width, rectUploadSettings.y + sizeButton + space * 2,
                rectUploadSettings.width / 2, sizeButton);
            
            var rectUploadSteamDescriptionToggle = new Rect(
                rectUploadSteamNameToggle.x, rectUploadSteamNameToggle.y + sizeButton + space,
                rectUploadSteamNameToggle.width / 2, sizeButton);
            
            var rectUploadSteamPreviewToggle = new Rect(
                rectUploadSteamDescriptionToggle.x, rectUploadSteamDescriptionToggle.y + sizeButton + space,
                rectUploadSteamDescriptionToggle.width, sizeButton);
            
            
            var rectUploadSteamName = new Rect(
                rectBuildSettings.x, rectUploadSteamNameToggle.y,
                rectBuildSettings.width, sizeButton);
            
            var rectUploadSteamDescription = new Rect(
                rectUploadSteamName.x, rectUploadSteamName.y + sizeButton + space,
                rectUploadSteamName.width, sizeButton);
            
            var rectUploadSteamPreview = new Rect(
                rectUploadSteamDescription.x, rectUploadSteamDescription.y + sizeButton + space,
                rectUploadSteamDescription.width, sizeButton);
            
            
            var rectBuildLocalName = new Rect(
                rectSceneName.x, rectUploadSteamPreviewToggle.y + sizeButton + space,
                rectBuildSettings.width / 2, sizeButton * 1.5f);
            
            var rectBuildLocal = new Rect(
                rectSceneName.x + rectBuildLocalName.width, rectBuildLocalName.y,
                rectBuildSettings.width / 2, sizeButton * 1.5f);
            
            var rectUploadButtons = new Rect(
                rectSplitBuild.x, rectBuildLocalName.y + sizeButton + space * 2,
                rectUploadSettings.width, sizeButton * 1.5f);

            var rectInfo = new Rect(
                rectButtons.x, rectUploadButtons.y + rectUploadButtons.height + space,
                rectButtons.width, sizeButton * 2);

            string message;

            if (attachObj != null && attachObj.metaConfig != null)
            {
                message = buildData.lastValid.ToString();
                var validComponentsHeight = EditorStyles.helpBox.CalcSize(new GUIContent(message)).y - space;
            
                var buildNames = Enum.GetNames(typeof(TempData));
                for (int i = 0; i < buildNames.Length; i++)
                {
                    var i1 = i;
                    var has = ((TempData)buildData.buildSuccess).HasFlag((TempData)Enum.Parse(typeof(TempData), buildNames[i]));
                    uploadState = has && uploadState;
                    if (!has)
                    {
                        Rect localRect = rectInfo;
                        m_queueActionDraw.Enqueue(() =>
                            EditorGUI.HelpBox(localRect, buildNames[i1] + " is not build", MessageType.Error));
                        rectInfo.y += rectInfo.height + space;
                    }
                    else if (buildNames[i] == TempData.Meta.ToString() && !buildData.lastMeta.Equals(attachObj.metaConfig.mapMetaConfigValue))
                    {
                        Rect localRect = rectInfo;
                        m_queueActionDraw.Enqueue(() =>
                            EditorGUI.HelpBox(localRect, $"Is Changed {buildNames[i1]}! Please build {buildNames[i1]}.", MessageType.Warning));
                        rectInfo.y += rectInfo.height + space;
                    }

                    if (buildNames[i] == TempData.Map.ToString() && !string.IsNullOrWhiteSpace(message))
                    {
                        rectInfo.height = validComponentsHeight;
                        Rect localRect = rectInfo;
                        m_queueActionDraw.Enqueue(() =>
                            EditorGUI.HelpBox(localRect, message, has ? MessageType.Info : MessageType.Error));
                        rectInfo.y += rectInfo.height + space;
                        rectInfo.height = 24;
                    }
                }
            }
            
            lastRect = rectInfo;
            
            var rectPreviewBack = new Rect(0, 0, rectPreview.width + 31, lastRect.y);
            var rectLabelId = new Rect(rectPreviewBack.width / 2 - 16, 2, 128, 24);
            m_scrollPositionPreview = GUI.BeginScrollView(
                new Rect(rectPreviewBack.x, 0, rectPreview.width + 44, position.height), m_scrollPositionPreview,
                new Rect(rectPreviewBack.x, 0, rectPreview.width, lastRect.y));
            
            EditorGUI.DrawRect(rectPreviewBack, new Color(0.22f, 0.22f, 0.22f));
            
            if (m_queueActionDraw != null)
            {
                while (m_queueActionDraw.Count > 0)
                {
                    m_queueActionDraw.Dequeue()?.Invoke(); 
                }
            }
            
            if (attachObj != null)
            {
                var old = attachObj.metaConfig;
                attachObj.metaConfig =
                    EditorGUI.ObjectField(rectConfig, old, typeof(MapMetaConfig), false) as MapMetaConfig;

                if (attachObj.metaConfig != null && !m_attahing[m_selectItem.Id])
                {
                    MapManagerConfig.Attach(m_selectItem.Id, attachObj.metaConfig);
                    m_attahing[m_selectItem.Id] = true;
                }
            }

            EditorGUI.DrawRect(rectPreview, Color.black);
            
            if (attachObj != null && attachObj.metaConfig != null)
            {
                GUI.DrawTexture(rectPreview, attachObj.metaConfig.mapMetaConfigValue.largeIcon);
            }
            else
            {
                EditorGUI.HelpBox(rectCenter, "Preview is missed", MessageType.Warning);
            }

            if (m_selectItem.Id != 0)
            {
                rectLabelId.x -= 16;
                GUI.Label(rectLabelId, m_selectItem.Id.ToString());
                rectLabelId.x -= 24;
                GUI.Label(rectLabelId, iconSteam);
            }
            var manager = MapManagerConfig.instance;
            
            if (attachObj != null && attachObj.metaConfig != null)
            {
                EditorGUI.BeginDisabledGroup(!isSelectAttach || 
                                             attachObj == null || 
                                             attachObj.metaConfig == null);
                
                GUI.Label(rectSplitLeft, "Build Targets");
                m_buildType = EditorGUI.MaskField(rectSplitRight, m_buildType, Enum.GetNames(typeof(TempData)));
                
                GUI.Box(rectBuildSettings, "Build Settings");

                EditorGUI.BeginDisabledGroup(m_buildType == 0);
                if (GUI.Button(rectSplitBuild, "Build") && !IsDownloadAnyIcon())
                {
                    m_loads[m_selectItem.Id] = true;
                    m_buildProcess = true;
                    var selectId = (ulong)m_selectItem.Id;
                    MapManagerConfig.instance.mapMetaConfigValue = attachObj.metaConfig;

                    MapBuilder.BuildCustom((TempData)m_buildType,
                        (TempData)buildData.buildSuccess,
                        selectId,
                        buildData.compress,
                        buildData.platform,
                        (path, success) 
                            => AddBuild(attachObj.metaConfig, buildData, path, success));
                }
                EditorGUI.EndDisabledGroup();
                
                var flagScene = ((TempData)m_buildType).HasFlag(TempData.Map);

                EditorGUI.BeginDisabledGroup(!flagScene);
                GUI.Label(rectSceneName, "Target Scene");
                var editorScenes = EditorBuildSettings.scenes;
                if (editorScenes.Length > 0)
                {
                    if (!flagScene && !string.IsNullOrWhiteSpace(buildData.targetScene))
                    {
                        MapManagerConfig.instance.targetScene = buildData.targetScene;
                    }
                    
                    int index = FindSceneIndex(editorScenes, MapManagerConfig.instance.targetScene);
                    var scenes = GetScenesName(editorScenes);
                    MapManagerConfig.instance.targetScene =
                        editorScenes[EditorGUI.Popup(rectScene, index, scenes)].path;
                }

                EditorGUI.EndDisabledGroup();

                var flagPlat = (buildData.buildSuccess & m_buildType) == buildData.buildSuccess;
                EditorGUI.BeginDisabledGroup(!flagPlat);
                GUI.Label(rectPlatformName, "Platform");
                m_platformBuild = (PlatformBuild)EditorGUI.EnumPopup(rectPlatform, flagPlat ? m_platformBuild : buildData.platform);
                EditorGUI.EndDisabledGroup();

                GUI.Label(rectBuildLocalName, "Local Build (*only test build)");
                
                var existItemDirectory = Directory.Exists(m_selectItem.Directory);
                EditorGUI.BeginDisabledGroup(!existItemDirectory);
                manager.buildLocal = EditorGUI.Toggle(rectBuildLocal, manager.buildLocal) && existItemDirectory;
                EditorGUI.EndDisabledGroup();

                rectBuildLocal.x += 22;
                rectBuildLocal.width -= 22;
                EditorGUI.HelpBox(rectBuildLocal, "don`t use the map restart", MessageType.Warning);
                //GUI.Label(rectCompressName, "Compression");
                //m_compressBuild = (CompressBuild)EditorGUI.EnumPopup(rectCompress, m_compressBuild);
                m_compressBuild = CompressBuild.NoCompress;
                
                GUI.Label(rectUploadSteamName, "Upload Name");
                GUI.Label(rectUploadSteamDescription, "Upload Description");
                GUI.Label(rectUploadSteamPreview, "Upload Preview");

                manager.uploadSteamName = EditorGUI.Toggle(rectUploadSteamNameToggle, manager.uploadSteamName);
                manager.uploadSteamDescription =
                    EditorGUI.Toggle(rectUploadSteamDescriptionToggle, manager.uploadSteamDescription);
                manager.uploadSteamPreview = EditorGUI.Toggle(rectUploadSteamPreviewToggle, manager.uploadSteamPreview);

                GUI.Box(rectUploadSettings, "Upload Settings");

                EditorGUI.BeginDisabledGroup(!uploadState);
                GUI.color = uploadState && isSelectAttach ? new Color(0.55f, 0.6f, 0.9f) : Color.white;
                if (GUI.Button(rectUploadButtons, manager.buildLocal ? "Upload to local exist" : "Upload to steam") && uploadState)
                {
                    m_loads[m_selectItem.Id] = true;
                    MapManagerConfig.instance.mapMetaConfigValue = attachObj.metaConfig;
                    MapBuilder.UploadCommunityFile(buildData,
                        m_selectItem, 
                        manager.buildLocal, 
                        id =>
                    {
                        m_loads[id] = false;
                        DownloadSpriteAsync(m_fetchResultListItems.Find(itemFind => itemFind.Id == id));
                    });
                }
                
                GUI.Label(rectUploadButtons, iconSteam);
                EditorGUI.EndDisabledGroup();
                GUI.color = Color.white;
                EditorGUI.EndDisabledGroup();
            }

            GUI.EndScrollView();
        }

        private void AddBuild(MapMetaConfig config, MapManagerConfig.BuildData buildData, string path, TempData complete)
        {
            m_loads[m_selectItem.Id] = false;
            if (complete == (TempData.Map | TempData.Meta))
            {
                Debug.Log($"Build Complete : Everything");
            }

            MapManagerConfig.AddBuild(new MapManagerConfig.BuildData(config, MapManagerConfig.instance.targetScene, path, (int)complete, ((TempData)m_buildType).HasFlag(TempData.Map)
                ? ModMapTestTool.Target
                : buildData.lastValid, m_platformBuild, m_compressBuild));
            m_buildProcess = false;
        }
        
        private string[] GetScenesName(EditorBuildSettingsScene[] editorScenes)
        {
            return editorScenes
                .Where(scene => scene.enabled)
                .Select(editorScene => MapBuilder.GetSceneNameFromPath(editorScene.path))
                .ToArray();
        }

        private int FindSceneIndex(EditorBuildSettingsScene[] scenes, string path)
        {
            for (int index = 0; index < scenes.Length; index++)
            {
                if (scenes[index].path == path && scenes[index].enabled)
                {
                    return index;
                }
            }

            return 0;
        }
        
        private bool IsDownloadAnyIcon()
        {
            foreach (var item in m_fetchResultListItems)
            {
                if (images[item.Id].Item2)
                {
                    return true;
                }
            }

            return false;
        }
    }
}