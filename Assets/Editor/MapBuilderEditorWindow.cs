using System;
using System.Collections.Generic;
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

        private Property m_configProperty;
        private int m_buildType;

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

        private void Fetch()
        {
            MapBuilder.InitSteamUGC();
            m_steamUgc = MapBuilder.steamUgc;
            FetchItems();
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

        private async Task FetchItems()
        {
            await Task.Delay(1000);
            await m_steamUgc.GetWorkshopItems(m_fetchResultListItems, DownloadSpriteAsync);
            
            foreach (var item in m_fetchResultListItems)
            {
                m_attahing[item.Id] = MapManagerConfig.IsAttach(item.Id);
            }
        }
        
        private async void DownloadSpriteAsync(Item item)
        {
            if (images.TryGetValue(item.Id, out var image) && image.Item2)
            {
                return;
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
        
        private void OnGUI()
        {
            const float aspect = 16.0f / 9.0f;
            const float sizeImage = 200;
            const float sizeButton = 18;
            var rectPreview = new Rect(16, 24, sizeImage * aspect, sizeImage);
            var rectCenter = new Rect((sizeImage * aspect) / 2f, sizeImage / 2f, 32f, 32f);
            var rectCenter2 = new Rect((sizeImage * aspect) / 2f - 64, sizeImage / 2f, 164f, 32f);
            var rectItem = new Rect(rectPreview.width + rectPreview.x + 16, 0, position.width - 32 - (rectPreview.width + rectPreview.x + 16), 28);
            const float space = 8;
            float elementHeight = space + rectItem.height;
            var iconSteam = EditorGUIUtility.IconContent("steam");
            bool uploadState = true;
            
            m_scrollPosition = GUI.BeginScrollView(
                new Rect(rectItem.x + space * 2, 0, rectItem.width + space * 2, position.height), m_scrollPosition, 
                new Rect(rectItem.x - 2, 0, rectItem.width, elementHeight * (m_fetchResultListItems.Count + 1) + space));
            
            m_attahing.TryGetValue(m_selectItem.Id, out var isSelectAttach);
            
            var loadIcon = EditorGUIUtility.IconContent(m_iconLoad[Mathf.FloorToInt((Time.time * 12) % m_iconLoad.Length)]);
            for (int i = 0; i < m_fetchResultListItems.Count; i++)
            {
                rectItem.y += space;
                var item = m_fetchResultListItems[i];
                GUI.Box(rectItem, string.Empty);
                if (GUI.Button(rectItem, string.Empty))
                {
                    m_selectItemIndex = i;
                }
                rectItem.x += 24;
                GUI.Label(rectItem, string.IsNullOrWhiteSpace(item.Title)? item.Id.ToString() : item.Title);
                rectItem.x -= 24;
                var rectItemWarning = new Rect(rectItem.x + rectItem.width - 48, rectItem.y, 48, 18);
                
                if (m_selectItemIndex == i)
                {
                    GUI.color = Color.black;
                    GUI.Box(rectItem, String.Empty);
                    GUI.color = Color.white;
                }

                if (!m_attahing.TryGetValue(item.Id, out var attach) || !attach)
                {
                    GUI.color = Color.red;
                    GUI.Box(rectItemWarning, "Detach");
                    GUI.color = Color.white;
                }
                
                rectItem.x += space / 2;
                if (m_loads.TryGetValue(item.Id, out var state) && state)
                {
                    GUI.Label(rectItem, loadIcon);
                }

                rectItem.x -= space / 2;
                rectItem.y += rectItem.height;
            }

            rectItem.y += space;
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
            
            var rectConfig = new Rect(
                rectPreview.x, rectPreview.y + rectPreview.height + 16,
                rectPreview.width, sizeButton);

            var rectConfigValue = new Rect(
                rectConfig.x, rectConfig.y + rectConfig.height + 4,
                rectConfig.width, sizeButton);
            
            var rectButtons = new Rect(
                rectConfigValue.x, rectConfigValue.y + rectConfigValue.height,
                rectConfigValue.width, sizeButton);

            SerializedObject prop = null;
            SerializedProperty propValue = null;
            MapManagerConfig.BuildData buildData = default;
            
            MapManagerConfig.GetOrAttach(m_selectItem.Id, out var attachObj);
            
            if (attachObj != null)
            {
                buildData = MapManagerConfig.GetBuildOrEmpty(m_selectItem.Id, attachObj.metaConfig);
            }

            if (attachObj != null && attachObj.metaConfig != null)
            {
                prop = new SerializedObject(attachObj.metaConfig);
                propValue = prop.FindProperty("mapMetaConfigValue");
                if (propValue != null)
                {
                    var propHeight = EditorGUI.GetPropertyHeight(propValue);
                    rectButtons.y = propHeight + rectPreview.height + rectPreview.y + space + 44;
                }
            }

            var rectSplitRight = new Rect(
                rectButtons.x + rectButtons.width / 2, rectButtons.y - 8,
                rectButtons.width / 2, sizeButton);

            var rectSplitLeft = new Rect(
                rectButtons.x, rectButtons.y - space,
                rectButtons.width / 2, sizeButton);

            var rectUploadButtons = new Rect(
                rectPreview.x, rectButtons.y + space * 2,
                rectPreview.width, sizeButton * 2);
            
            string message = string.Empty;
            
            float validComponentsHeight = 0f;
            float messageHeight = 0f;
            
            if (attachObj != null)
            {
                message = buildData.lastValid.ToString();
                int numLines = message.Split('\n').Length;
                validComponentsHeight = 12 * numLines;
                messageHeight = (Enum.GetValues(typeof(TempData))
                    .Cast<Enum>()
                    .Count(val =>!((TempData)buildData.buildSuccess).HasFlag(val))) * (rectUploadButtons.height + space);
            }
            
            var rectPreviewBack = new Rect(12, 4, rectPreview.width + 8, rectButtons.y + messageHeight + validComponentsHeight + 52);
            var rectLabelId = new Rect(rectPreviewBack.width / 2 - 16, 2, 128, 24);
            m_scrollPositionPreview = GUI.BeginScrollView(
                new Rect(rectPreviewBack.x, 0, rectPreview.width + 24, position.height), m_scrollPositionPreview,
                new Rect(rectPreviewBack.x, 0, rectPreview.width, rectButtons.y + messageHeight + validComponentsHeight + 52));

            if (attachObj != null)
            {
                var old = attachObj.metaConfig;
                attachObj.metaConfig =
                    EditorGUI.ObjectField(rectConfig, old, typeof(MapMetaConfig), false) as MapMetaConfig;

                if (old == null || attachObj.metaConfig == null || 
                    attachObj.metaConfig.mapMetaConfigValue.itemWorkshopId != old.mapMetaConfigValue.itemWorkshopId)
                {
                    if (old != null)
                    {
                        MapManagerConfig.Detach(old.mapMetaConfigValue.itemWorkshopId);
                        m_attahing[old.mapMetaConfigValue.itemWorkshopId] = true;
                    }
                    
                    if (attachObj.metaConfig != null)
                    {
                        MapManagerConfig.Attach(m_selectItem.Id, attachObj.metaConfig);
                        m_attahing[m_selectItem.Id] = true;
                    }
                }
            }

            if (propValue != null)
            {
                EditorGUI.PropertyField(rectConfigValue, propValue, true);
                prop.ApplyModifiedProperties();
            }
            
            GUI.Box(rectPreviewBack, string.Empty);
            EditorGUI.DrawRect(rectPreview, Color.black);
            if (images.TryGetValue(m_selectItem.Id, out var attachData) && !attachData.Item2)
            {
                if(attachData.Item1 != null && attachData.Item1.width > 1)
                {
                    GUI.DrawTexture(rectPreview, attachData.Item1);
                }
                else
                {
                    EditorGUI.HelpBox(rectCenter2, "Steam preview is missed", MessageType.Warning);
                }
                
                if (attachData.Item1 == null)
                {
                    DownloadSpriteAsync(m_selectItem);
                }
            }
            else
            {
                GUI.Label(rectCenter, loadIcon);
            }

            if (m_selectItem.Id != 0)
            {
                GUI.Label(rectLabelId, m_selectItem.Id.ToString());
                rectLabelId.x -= 24;
                GUI.Label(rectLabelId, iconSteam);
            }

            EditorGUI.BeginDisabledGroup(!isSelectAttach);

            m_buildType = EditorGUI.MaskField(rectSplitRight, m_buildType, Enum.GetNames(typeof(TempData)));
            
            GUI.color = Color.white;
            if (GUI.Button(rectSplitLeft, "Build") && !IsDownloadAnyIcon())
            {
                m_loads[m_selectItem.Id] = true;
                int buildType = m_buildType;
                if (attachObj != null && attachObj.metaConfig != null && buildType != 0)
                {
                    MapManagerConfig.instance.mapMetaConfigValue = attachObj.metaConfig;
                    MapManagerConfig.ClearBuild(m_selectItem.Id, attachObj.metaConfig);
                    
                    MapBuilder.BuildCustom((TempData)buildType, (TempData)buildData.buildSuccess, m_selectItem.Id,
                        (path,complete) =>
                        {
                            m_loads[m_selectItem.Id] = false;
                            if (attachObj != null)
                            {
                                MapManagerConfig.AddBuild(new MapManagerConfig.BuildData(m_selectItem.Id,
                                    attachObj.metaConfig, path, (int)complete, ModMapTestTool.Target));
                            }
                            
                            Fetch();
                        });
                }
            };
            
            GUI.color = Color.white;
            if (attachObj != null)
            {
                var buildNames = Enum.GetNames(typeof(TempData));
                for (int i = 0; i < buildNames.Length; i++)
                {
                    var has = ((TempData)buildData.buildSuccess).HasFlag((TempData)Enum.Parse(typeof(TempData), buildNames[i]));
                    uploadState = has && uploadState;
                    if (!has)
                    {
                        EditorGUI.HelpBox(rectUploadButtons, buildNames[i] + " is not build", MessageType.Error);
                        rectUploadButtons.y += rectUploadButtons.height + space;
                    }
                    
                    if (buildNames[i] == TempData.Map.ToString() && !string.IsNullOrWhiteSpace(message))
                    {
                        rectUploadButtons.height = validComponentsHeight;
                        EditorGUI.HelpBox(rectUploadButtons, message, has ? MessageType.Info : MessageType.Error);
                        rectUploadButtons.y += rectUploadButtons.height + space;
                        rectUploadButtons.height = 24;
                    }
                }
            }
            EditorGUI.BeginDisabledGroup(!uploadState);
            GUI.color = uploadState && isSelectAttach ? new Color(0.55f, 0.6f, 0.9f) : Color.white;
            if (GUI.Button(rectUploadButtons, "Upload to steam") && uploadState)
            {
                m_loads[m_selectItem.Id] = true;
                MapBuilder.UploadCommunityFile(m_selectItem.Id, item =>
                {
                    m_loads[item] = false;
                });
            };
            GUI.Label(rectUploadButtons, iconSteam);
            EditorGUI.EndDisabledGroup();
            GUI.color = Color.white;
            EditorGUI.EndDisabledGroup();
            GUI.EndScrollView();
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