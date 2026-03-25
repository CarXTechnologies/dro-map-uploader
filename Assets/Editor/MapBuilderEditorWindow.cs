using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Steamworks;
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
		private readonly List<Item> m_fetchResultListItems = new();

		private Item SelectItem => m_selectItemIndex >= 0 && m_selectItemIndex < m_fetchResultListItems.Count ? m_fetchResultListItems[m_selectItemIndex] : default;

		private readonly Dictionary<ulong, bool> m_loads = new();
		private readonly Dictionary<ulong, bool> m_attaching = new();
		private readonly Dictionary<ulong, (Texture2D, bool)> m_images = new();

		private readonly Queue<Action> m_queueActionDraw = new();

		private Property m_configProperty;
		private int m_buildType;
		private FormatBuild m_buildFormat;
		private FormatBuild m_buildFormatCached;
		private PlatformBuild m_platformBuild;
		private CompressBuild m_compressBuild;
		private PlatformBuild m_platformBuildCached;
		private CompressBuild m_compressBuildCached;
		private bool m_buildProcess;
		private int m_selectionUploadSetting;
		private bool m_buttonLastClickOnAnyItem = true;

		private enum UploadSettingVariant
		{
			Steam = 0,
			External = 1,
		}

		private readonly string[] m_iconLoad =
		{
			"d_WaitSpin00",
			"d_WaitSpin01",
			"d_WaitSpin02",
			"d_WaitSpin03",
			"d_WaitSpin04",
			"d_WaitSpin05",
			"d_WaitSpin06",
			"d_WaitSpin07",
			"d_WaitSpin08",
			"d_WaitSpin09",
			"d_WaitSpin10",
			"d_WaitSpin11"
		};

		private string m_pathToExternal;

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
			MapBuilder.InitSteamUgc();
			m_steamUgc = MapBuilder.steamUgc;
			await FetchItems();
		}

		private void OnDisable()
		{
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
				DestroyImmediate(image.Value.Item1);
			}
		}

		private async Task FetchItems()
		{
			if (!SteamClient.IsValid)
			{
				return;
			}

			while (m_buildProcess)
			{
				await Task.Delay(100);
			}

			await m_steamUgc.GetWorkshopItems(m_fetchResultListItems, DownloadSpriteAsync);

			foreach (var item in m_fetchResultListItems)
			{
				m_attaching[item.Id] = MapManagerConfig.IsAttach(item.Id);
			}

			MapManagerConfig.ValidBuildsAndAttaching(m_fetchResultListItems);
		}

		private async void DownloadSpriteAsync(Item item)
		{
			if (m_images.TryGetValue(item.Id, out var image) && image.Item2)
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

			if (string.IsNullOrWhiteSpace(item.PreviewImageUrl))
			{
				return;
			}

			m_images[item.Id] = (null, true);
			await UIUtils.DownloadSprite(item.PreviewImageUrl, (_,
				texture2D) =>
			{
				m_images[item.Id] = (texture2D == null ? new Texture2D(1, 1) : texture2D, false);
			});
		}

		[Obsolete("Obsolete")]
		private void OnGUI()
		{
			GUI.skin.box.normal.textColor = Color.white;
			const float aspect = 16.0f / 9.0f;
			const float sizeImage = 200;
			const float sizeButton = 18;
			const float space = 6;

			var rectPreview = new Rect(16, 28, sizeImage * aspect, sizeImage);
			var rectCenterPreview = new Rect((sizeImage * aspect) / 2f - 64, sizeImage / 2f, 164f, 32f);
			var rectItem = new Rect(rectPreview.width + rectPreview.x + 32, 0, position.width - 48 - (rectPreview.width + rectPreview.x + 16), 80);
			var rectWindow = rootVisualElement.contentRect;
			float elementHeight = space + rectItem.height;
			var iconSteam = EditorGUIUtility.IconContent("steam");
			bool uploadState = true;

			if (!SteamClient.IsValid)
			{
				EditorGUI.HelpBox(new Rect(rectWindow.width / 2, rectWindow.height / 2, 128, 64), "Please open steam", MessageType.Error);
				return;
			}

			m_scrollPosition = GUI.BeginScrollView(new Rect(rectItem.x + space * 2, 0, rectItem.width + space * 3, position.height),
				m_scrollPosition, new Rect(rectItem.x - 2, 0, rectItem.width, elementHeight * (m_fetchResultListItems.Count + 2) + space));

			m_attaching.TryGetValue(SelectItem.Id, out var isSelectAttach);

			MapManagerConfig.BuildData buildData = default;

			MapManagerConfig.GetOrAttach(SelectItem.Id, out var attachObj);

			if (attachObj != null)
			{
				buildData = MapManagerConfig.GetBuildOrEmpty(attachObj.metaConfig);
				if (m_buttonLastClickOnAnyItem)
				{
					m_compressBuild = buildData.compress;
					m_platformBuild = buildData.platform;
					m_buildType = buildData.buildSuccess;
					MapManagerConfig.instance.targetScene = buildData.targetScene;
					m_buttonLastClickOnAnyItem = false;
				}
			}

			var loadIcon = EditorGUIUtility.IconContent(m_iconLoad[Mathf.FloorToInt((Time.time * 12) % m_iconLoad.Length)]);

			if (GUI.Button(rectItem, "Fetch steam workshop"))
			{
				Fetch();
			}

			rectItem.y += rectItem.height;

			for (int i = 0; i < m_fetchResultListItems.Count; i++)
			{
				rectItem.y += space;
				var item = m_fetchResultListItems[i];

				GUI.Box(rectItem, string.Empty);

				var hasOldFlag = item.HasTag(SteamUGCManager.MAP_TAG_OLD);
				if (hasOldFlag)
				{
					GUI.color = new Color(0.2f, 0.5f, 0.3f, 1f);
				}

				if (GUI.Button(rectItem, hasOldFlag ? "Old version!" : string.Empty))
				{
					m_buttonLastClickOnAnyItem = true;
					m_selectItemIndex = i;
				}

				GUI.color = Color.white;
				rectItem.x += rectItem.height + space;
				rectItem.height /= 2;
				var oldSize = GUI.skin.label.fontSize;
				GUI.skin.label.fontSize = 16;
				GUI.skin.label.fontStyle = FontStyle.Bold;
				GUI.Label(rectItem, string.IsNullOrWhiteSpace(item.Title) ? $"Blank {i}" : item.Title);

				rectItem.y += rectItem.height;
				var itemDetail = SteamUGCManager.GetItemDetail(item);

				GUI.skin.label.fontStyle = FontStyle.Normal;
				GUI.skin.label.fontSize = 12;
				GUI.Label(rectItem, $"{Mathf.FloorToInt(itemDetail.FileSize / ModMapTestTool.BYTES_TO_MEGABYTES)} / " + $"{(ModMapTestTool.Steam.maxSizeInMb + ModMapTestTool.Steam.maxSizeInMbMeta)} mb");
				rectItem.y -= rectItem.height;
				rectItem.height *= 2;
				rectItem.x -= rectItem.height + space;

				var rectItemWarning = new Rect(rectItem.x + rectItem.width - 48, rectItem.y, 48, 18);
				GUI.skin.label.fontSize = oldSize;

				if (m_selectItemIndex == i)
				{
					GUI.color = Color.black;
					GUI.Box(rectItem, string.Empty);
					GUI.color = Color.white;
				}

				if (!MapManagerConfig.TryGetAttach(m_fetchResultListItems[i].Id, out var attachData) || attachData.metaConfig == null)
				{
					GUI.color = Color.red;
					GUI.Box(rectItemWarning, "Detach");
					GUI.color = Color.white;
				}

				rectItem.x += space;

				var rectItemPreview = rectItem;
				rectItemPreview.width = 128 * (9f / 16f);

				if (m_images.TryGetValue(item.Id, out var imageData) && !imageData.Item2)
				{
					if (imageData.Item1 != null && imageData.Item1.width > 1)
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
			}

			GUI.Label(rectItem, iconSteam);
			GUI.EndScrollView();

			Rect lastRect;

			var rectConfig = new Rect(
				rectPreview.x,
				rectPreview.y + rectPreview.height + space,
				rectPreview.width,
				sizeButton);

			var rectConfigValue = new Rect(
				rectConfig.x,
				rectConfig.y + rectConfig.height,
				rectConfig.width,
				sizeButton);

			var rectButtons = new Rect(
				rectConfigValue.x,
				rectConfigValue.y + space * 2,
				rectConfigValue.width,
				sizeButton);

			var rectBuildSettings = new Rect(
				rectPreview.x,
				rectButtons.y - space,
				rectPreview.width,
				sizeButton + space / 2);

			var rectPlatform = new Rect(
				rectBuildSettings.x + rectBuildSettings.width / 2,
				rectBuildSettings.y + sizeButton + space * 2,
				rectBuildSettings.width / 2,
				sizeButton);

			var rectFormat = new Rect(
				rectPlatform.x,
				rectPlatform.y + sizeButton + space * 2,
				rectBuildSettings.width / 2,
				sizeButton);

			var rectScene = new Rect(
				rectFormat.x,
				rectFormat.y + (sizeButton + space),
				rectFormat.width,
				sizeButton);

			var rectCompress = new Rect(
				rectScene.x,
				rectScene.y + sizeButton + space,
				rectScene.width,
				sizeButton);

			var rectPlatformName = new Rect(
				rectBuildSettings.x,
				rectPlatform.y,
				rectBuildSettings.width / 2,
				sizeButton);

			var rectFormatName = new Rect(
				rectBuildSettings.x,
				rectFormat.y,
				rectBuildSettings.width / 2,
				sizeButton);

			var rectCompressName = new Rect(
				rectPlatformName.x,
				rectCompress.y,
				rectPlatformName.width,
				sizeButton);

			var rectSceneName = new Rect(
				rectPlatformName.x,
				rectCompressName.y - (sizeButton + space),
				rectPlatformName.width,
				sizeButton);

			var rectSplitRight = new Rect(
				rectCompressName.x + rectCompressName.width,
				rectCompressName.y + sizeButton + space,
				rectCompressName.width,
				sizeButton);

			var rectSplitLeft = new Rect(
				rectCompressName.x,
				rectCompressName.y + sizeButton + space,
				rectCompressName.width,
				sizeButton);

			lastRect = rectSplitLeft;

			var rectSplitBuild = new Rect(
				rectCompressName.x,
				lastRect.y + sizeButton + space,
				rectBuildSettings.width,
				sizeButton * 1.5f);

			var rectUploadSettings = new Rect(
				rectBuildSettings.x,
				rectSplitBuild.y + sizeButton + space * 3,
				rectBuildSettings.width,
				sizeButton + space / 2);

			ShowBuildResultIfExists(ref uploadState,
				ref rectUploadSettings,
				attachObj,
				buildData,
				space);

			var rectSelectionGridSettings = new Rect(
				rectBuildSettings.x,
				rectUploadSettings.y + sizeButton + space * 3,
				rectBuildSettings.width,
				sizeButton + space / 2);

			var rectUploadExternalNameFolder = new Rect(
				rectUploadSettings.x + rectUploadSettings.width * 0.9f,
				rectSelectionGridSettings.y + sizeButton + space * 2,
				rectUploadSettings.width * 0.1f,
				sizeButton);

			var rectUploadSteamNameToggle = new Rect(
				rectUploadSettings.x + rectCompressName.width,
				rectSelectionGridSettings.y + sizeButton + space * 2,
				rectUploadSettings.width / 2,
				sizeButton);

			var rectUploadSteamDescriptionToggle = new Rect(
				rectUploadSteamNameToggle.x,
				rectUploadSteamNameToggle.y + sizeButton + space,
				rectUploadSteamNameToggle.width / 2,
				sizeButton);

			var rectUploadSteamPreviewToggle = new Rect(
				rectUploadSteamDescriptionToggle.x,
				rectUploadSteamDescriptionToggle.y + sizeButton + space,
				rectUploadSteamDescriptionToggle.width,
				sizeButton);


			var rectUploadExternalName = new Rect(
				rectBuildSettings.x,
				rectUploadSteamNameToggle.y,
				rectBuildSettings.width * 0.9f,
				sizeButton);

			var rectUploadToExternalPath = new Rect(
				rectBuildSettings.x,
				rectUploadExternalName.y + sizeButton + space,
				rectBuildSettings.width,
				sizeButton * 1.5f);

			var rectUploadSteamName = new Rect(
				rectBuildSettings.x,
				rectUploadSteamNameToggle.y,
				rectBuildSettings.width,
				sizeButton);

			var rectUploadSteamDescription = new Rect(
				rectUploadSteamName.x,
				rectUploadSteamName.y + sizeButton + space,
				rectUploadSteamName.width,
				sizeButton);

			var rectUploadSteamPreview = new Rect(
				rectUploadSteamDescription.x,
				rectUploadSteamDescription.y + sizeButton + space,
				rectUploadSteamDescription.width,
				sizeButton);


			var rectBuildLocalName = new Rect(
				rectSceneName.x,
				rectUploadSteamPreviewToggle.y + sizeButton + space,
				rectBuildSettings.width / 2,
				sizeButton * 1.5f);

			var rectBuildLocal = new Rect(
				rectSceneName.x + rectBuildLocalName.width,
				rectBuildLocalName.y,
				rectBuildSettings.width / 2,
				sizeButton * 1.5f);

			var rectUploadButtons = new Rect(
				rectSplitBuild.x,
				rectBuildLocalName.y + sizeButton + space * 2,
				rectUploadSettings.width,
				sizeButton * 1.5f);

			var rectInfo = new Rect(
				rectButtons.x,
				rectUploadButtons.y + rectUploadButtons.height + space,
				rectButtons.width,
				sizeButton * 2);

			lastRect = (UploadSettingVariant)m_selectionUploadSetting == UploadSettingVariant.Steam ? rectInfo : new Rect(rectUploadToExternalPath.position + Vector2.up * sizeButton * 2f, rectUploadToExternalPath.size);

			var rectPreviewBack = new Rect(0, 0, rectPreview.width + 31, lastRect.y);
			var rectLabelId = new Rect(rectPreviewBack.width / 2 - 16, 2, 128, 24);

			m_scrollPositionPreview = GUI.BeginScrollView(new Rect(rectPreviewBack.x, 0, rectPreview.width + 44, position.height), m_scrollPositionPreview,
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
				attachObj.metaConfig = EditorGUI.ObjectField(rectConfig, old, typeof(MapMetaConfig), false) as MapMetaConfig;

				if (attachObj.metaConfig != null && m_attaching != null && m_attaching.ContainsKey(SelectItem.Id) && !m_attaching[SelectItem.Id])
				{
					MapManagerConfig.Attach(SelectItem.Id, attachObj.metaConfig);
					m_attaching[SelectItem.Id] = true;
				}
			}

			EditorGUI.DrawRect(rectPreview, Color.black);

			if (attachObj != null && attachObj.metaConfig != null)
			{
				GUI.DrawTexture(rectPreview, attachObj.metaConfig.mapMetaConfigValue.largeIcon);
			}
			else
			{
				EditorGUI.HelpBox(rectCenterPreview, "Preview is missed", MessageType.Warning);
			}

			if (SelectItem.Id != 0)
			{
				rectLabelId.x -= 16;
				GUI.Label(rectLabelId, SelectItem.Id.ToString());
				rectLabelId.x -= 24;
				GUI.Label(rectLabelId, iconSteam);
			}

			var manager = MapManagerConfig.instance;

			if (attachObj != null && attachObj.metaConfig != null)
			{
				EditorGUI.BeginDisabledGroup(!isSelectAttach || attachObj == null || attachObj.metaConfig == null);

				GUI.Label(rectSplitLeft, "Build Targets");
				m_buildType = EditorGUI.MaskField(rectSplitRight, m_buildType, Enum.GetNames(typeof(TempData)));

				GUI.Box(rectBuildSettings, "Build Settings");

				EditorGUI.BeginDisabledGroup(m_buildType == 0);
				if (GUI.Button(rectSplitBuild, "Build") && !IsDownloadAnyIcon())
				{
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
					MapManagerConfig.instance.targetScene = editorScenes[EditorGUI.Popup(rectScene, index, scenes)].path;
				}

				EditorGUI.EndDisabledGroup();

				var flagPlat = (buildData.buildSuccess & m_buildType) == buildData.buildSuccess;
				EditorGUI.BeginDisabledGroup(!flagPlat);
				GUI.Label(rectPlatformName, "Platform");
				m_platformBuild = (PlatformBuild)EditorGUI.EnumPopup(rectPlatform, flagPlat ? m_platformBuild : buildData.platform);

				GUI.Label(rectFormatName, "Format");
				m_buildFormat = (FormatBuild)EditorGUI.EnumPopup(rectFormat, flagPlat ? m_buildFormat : buildData.format);

				EditorGUI.EndDisabledGroup();

				var variants = Enum.GetNames(typeof(UploadSettingVariant));

				m_selectionUploadSetting = GUI.SelectionGrid(rectSelectionGridSettings, m_selectionUploadSetting, variants, variants.Length);

				GUI.Box(rectUploadSettings, "Upload Settings");

				GUI.Label(rectCompressName, "Compression");
				m_compressBuild = (CompressBuild)EditorGUI.EnumPopup(rectCompress, m_compressBuild);

				if ((UploadSettingVariant)m_selectionUploadSetting == UploadSettingVariant.Steam)
				{
					GUI.Label(rectBuildLocalName, "Local Build (*only test build)");

					var existItemDirectory = Directory.Exists(SelectItem.Directory);
					EditorGUI.BeginDisabledGroup(!existItemDirectory);
					manager.buildLocal = EditorGUI.Toggle(rectBuildLocal, manager.buildLocal) && existItemDirectory;
					EditorGUI.EndDisabledGroup();

					rectBuildLocal.x += 22;
					rectBuildLocal.width -= 22;

					if (existItemDirectory)
					{
						EditorGUI.HelpBox(rectBuildLocal, "don`t use the map restart", MessageType.Warning);
					}
					else
					{
						EditorGUI.HelpBox(rectBuildLocal, "need subscribe this item", MessageType.Error);
					}

					GUI.Label(rectUploadSteamName, "Upload Name");
					GUI.Label(rectUploadSteamDescription, "Upload Description");
					GUI.Label(rectUploadSteamPreview, "Upload Preview");

					manager.uploadSteamName = EditorGUI.Toggle(rectUploadSteamNameToggle, manager.uploadSteamName);
					manager.uploadSteamDescription = EditorGUI.Toggle(rectUploadSteamDescriptionToggle, manager.uploadSteamDescription);
					manager.uploadSteamPreview = EditorGUI.Toggle(rectUploadSteamPreviewToggle, manager.uploadSteamPreview);

					EditorGUI.BeginDisabledGroup(!uploadState);
					GUI.color = uploadState && isSelectAttach ? new Color(0.55f, 0.6f, 0.9f) : Color.white;

					if (GUI.Button(rectUploadButtons, manager.buildLocal ? "Upload to local exist" : "Upload to steam") && uploadState)
					{
						m_loads[SelectItem.Id] = true;
						MapManagerConfig.instance.mapMetaConfigValue = attachObj.metaConfig;
						MapBuilder.UploadSteamCommunityItem(buildData,
							SelectItem,
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
				else
				{
					m_pathToExternal = GUI.TextField(rectUploadExternalName, m_pathToExternal);

					if (GUI.Button(rectUploadExternalNameFolder, EditorGUIUtility.IconContent("d_FolderOpened Icon")))
					{
						m_pathToExternal = EditorUtility.SaveFolderPanel("External path", Application.streamingAssetsPath, attachObj.id.ToString());
					}

					EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(m_pathToExternal) || !Path.IsPathFullyQualified(m_pathToExternal));

					if (GUI.Button(rectUploadToExternalPath, "Upload to external folder"))
					{
						MapBuilder.BuildDataTransitionToDirectory(buildData, m_pathToExternal);
					}

					EditorGUI.EndDisabledGroup();
				}
			}

			GUI.EndScrollView();

			GUI.color = new Color(1f, 1f, 1f, .7f);

			var version = GameVersion.GetFullVersion();
			var versionSize = GUI.skin.label.CalcSize(new GUIContent(version));

			EditorGUI.DropShadowLabel(new Rect(position.width - sizeButton * 2 - versionSize.x, position.height - sizeButton * 2, versionSize.x + sizeButton, sizeButton), version);
			GUI.color = Color.white;
		}

		private void ShowBuildResultIfExists(ref bool uploadState,
			ref Rect rectInfo,
			MapManagerConfig.AttachData attachObj,
			MapManagerConfig.BuildData buildData,
			float space)
		{
			if (attachObj == null || attachObj.metaConfig == null)
			{
				return;
			}

			var message = buildData.lastValid.ToString();
			var validComponentsHeight = EditorStyles.helpBox.CalcSize(new GUIContent(message)).y + space;

			var buildNames = Enum.GetNames(typeof(TempData));

			for (int i = 0; i < buildNames.Length; i++)
			{
				var has = ((TempData)buildData.buildSuccess).HasFlag((TempData)Enum.Parse(typeof(TempData), buildNames[i]));
				uploadState = has && uploadState;

				if (!has)
				{
					EnqueueDrawAction(ref rectInfo, buildNames[i] + " is not build", MessageType.Error);
				}
				else if (buildNames[i] == nameof(TempData.Meta) && !buildData.lastMeta.Equals(attachObj.metaConfig.mapMetaConfigValue))
				{
					EnqueueDrawAction(ref rectInfo, $"Is Changed {buildNames[i]}! Please build {buildNames[i]}.", MessageType.Warning);
				}

				if (buildNames[i] == nameof(TempData.Map) && !string.IsNullOrWhiteSpace(message))
				{
					rectInfo.height = validComponentsHeight;
					EnqueueDrawAction(ref rectInfo, message, has ? MessageType.Info : MessageType.Error);
					rectInfo.height = 24;
				}
			}

			void EnqueueDrawAction(ref Rect rect, string msg, MessageType messageType)
			{
				var localRect = rect;
				m_queueActionDraw.Enqueue(() => EditorGUI.HelpBox(localRect, msg, messageType));
				rect.y += rect.height + space;
			}
		}

		private void AddBuild(MapMetaConfig config,
			MapManagerConfig.BuildData buildData,
			string path,
			TempData complete)
		{
			m_loads[SelectItem.Id] = false;

			if (complete == (TempData.Map | TempData.Meta))
			{
				Debug.Log($"Build Complete : Everything");
			}

			buildData.compress = m_compressBuildCached;
			buildData.platform = m_platformBuildCached;

			MapManagerConfig.AddBuild(new MapManagerConfig.BuildData(config,
				MapManagerConfig.instance.targetScene,
				path, (int)complete,
				((TempData)m_buildType).HasFlag(TempData.Map) ? ModMapTestTool.Target : buildData.lastValid,
				m_buildFormat, m_platformBuildCached, m_compressBuildCached));

			m_buildProcess = false;
		}

		private string[] GetScenesName(EditorBuildSettingsScene[] editorScenes)
		{
			return editorScenes
				.Where(scene => scene.enabled)
				.Select(editorScene => MapBuilder.GetSceneNameFromPathNoId(editorScene.path))
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
				if (m_images.TryGetValue(item.Id, out var itemImage) && itemImage.Item2)
				{
					return true;
				}
			}

			return false;
		}
	}
}