using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MapUploader.DocsCapture
{
	/// <summary>
	/// Maintainer-only tool that captures the raw screenshots for docs/images. It lives in docs/_capture/Editor and is copied
	/// into Assets/Editor/DocsCapture only while capturing (see docs/_capture/README.md). Each shot is saved as a raw PNG of
	/// the whole window plus a JSON file with the rectangles of every visible UI Toolkit element; docs/_capture/annotate.py
	/// crops, numbers and compresses them into docs/images.
	/// The run never saves a scene and restores every asset, EditorPrefs key and gizmo setting it touches.
	/// </summary>
	public static class DocsCapture
	{
		private const string ScenePath = "Assets/MapResources/TestMap/TestMap.unity";
		private const string ConfigPath = "Assets/MapResources/TestMap/TestMapConfig.asset";
		private const string ManagerConfigPath = "Assets/Resources/MapManagerConfig.asset";
		private const string TemplateConfigPath = "Assets/Resources/GameMarkerTemplateConfig.asset";
		private const string MaterialPath = "Assets/MapResources/TestMap/Art/0x4F368125.mat";
		private const string LanguagePreference = "CarX.MapBuilder.Language";
		private const string HdrpLitUiStateKey = "CoreRP:Material:UI_State:HDRP/Lit";
		private const string ExportFolderExample = "C:/Games/CarX Drift Racing Online 2/Mods/TestMap";
		private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
		private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

		private static readonly Stack<IEnumerator> s_routines = new();
		private static bool s_exitWhenDone;
		private static bool s_failed;
		private static string s_outDir;
		private static StringBuilder s_log;
		private static readonly Dictionary<string, byte[]> s_fileSnapshot = new();
		private static readonly Dictionary<string, string> s_prefsSnapshot = new();
		private static readonly Dictionary<Type, bool> s_gizmoSnapshot = new();
		private static readonly List<EditorWindow> s_openedWindows = new();
		private static readonly HashSet<string> s_only = new();

		private static Type MapBuilderWindowType => typeof(global::Editor.MapBuilderEditorWindow);

		// ---------- entry points ----------

		/// <summary>Command line: -executeMethod MapUploader.DocsCapture.DocsCapture.Run [-docsCaptureOut path] [-docsCaptureOnly id,id] [-docsCaptureExit]</summary>
		public static void Run()
		{
			var args = Environment.GetCommandLineArgs();
			s_exitWhenDone = args.Contains("-docsCaptureExit");
			s_outDir = ArgValue(args, "-docsCaptureOut") ?? Path.Combine(ProjectRoot, "Library", "DocsCapture");
			s_only.Clear();
			var only = ArgValue(args, "-docsCaptureOnly");
			if (!string.IsNullOrEmpty(only))
			{
				foreach (var id in only.Split(',')) s_only.Add(id.Trim());
			}

			Start(RunAll());
		}

		[MenuItem("Tools/Docs/Capture screenshots")]
		private static void RunFromMenu()
		{
			s_exitWhenDone = false;
			s_outDir = Path.Combine(ProjectRoot, "Library", "DocsCapture");
			s_only.Clear();
			Start(RunAll());
		}

		private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

		private static string ArgValue(string[] args, string name)
		{
			int i = Array.IndexOf(args, name);
			return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
		}

		private static bool Wanted(string id) => s_only.Count == 0 || s_only.Contains(id);

		// ---------- coroutine runner (EditorApplication.update) ----------

		private static void Start(IEnumerator routine)
		{
			s_log = new StringBuilder();
			s_failed = false;
			s_routines.Clear();
			s_routines.Push(routine);
			EditorApplication.update -= Tick;
			EditorApplication.update += Tick;
			Log($"DocsCapture started, out = {s_outDir}");
		}

		private static void Tick()
		{
			if (s_routines.Count == 0)
			{
				EditorApplication.update -= Tick;
				return;
			}

			var top = s_routines.Peek();
			bool moved;
			try
			{
				moved = top.MoveNext();
			}
			catch (Exception e)
			{
				s_failed = true;
				Log("EXCEPTION: " + e);
				s_routines.Clear();
				s_routines.Push(Finish());
				return;
			}

			if (!moved)
			{
				s_routines.Pop();
				return;
			}

			if (top.Current is IEnumerator nested) s_routines.Push(nested);
		}

		private static void Log(string message)
		{
			Debug.Log("[DocsCapture] " + message);
			s_log?.AppendLine($"{DateTime.Now:HH:mm:ss} {message}");
			if (s_outDir != null)
			{
				Directory.CreateDirectory(s_outDir);
				File.WriteAllText(Path.Combine(s_outDir, "capture.log"), s_log?.ToString() ?? string.Empty);
			}
		}

		private static IEnumerator Wait(int ticks)
		{
			for (int i = 0; i < ticks; i++) yield return null;
		}

		private static IEnumerator WaitUntil(Func<bool> condition, double timeoutSeconds, string what)
		{
			double end = EditorApplication.timeSinceStartup + timeoutSeconds;
			while (!condition())
			{
				if (EditorApplication.timeSinceStartup > end)
				{
					Log($"timeout waiting for {what}");
					yield break;
				}

				yield return null;
			}
		}

		private static IEnumerator Settle(EditorWindow w, int ticks = 3)
		{
			for (int i = 0; i < ticks; i++)
			{
				if (w != null && w.GetType() == MapBuilderWindowType) AnonymizeAccount(w);
				if (w != null) w.Repaint();
				yield return null;
			}

			if (w != null) w.Repaint();
			yield return null;
		}

		// ---------- the run ----------

		private static IEnumerator RunAll()
		{
			yield return Wait(5);
			Preflight();
			Snapshot();
			CloseToolWindows();
			yield return Wait(2);

			EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
			yield return Wait(3);
			ApplyDemoMetadata();


			foreach (var lang in new[] { "en", "ru" })
			{
				if (Wanted("window-overview") || Wanted("map-settings")) yield return MapTabShots(lang);
				if (Wanted("build-tab") || Wanted("build-optimization") || Wanted("export-folder") || Wanted("publish-modio")) yield return BuildAndPublishShots(lang);
			}

			if (Wanted("validation-window")) yield return ValidationShot();
			if (Wanted("scene-hdrp-lit-material")) yield return MaterialShot();
			if (Wanted("markers-road-template")) yield return RoadMarkerShot();
			if (Wanted("markers-template-config")) yield return TemplateConfigShot();
			if (Wanted("markers-material")) yield return MaterialMarkerShot();
			if (Wanted("minimap-component")) yield return MinimapShot();
			if (Wanted("minimap-capture-camera")) yield return CaptureCameraShot();
			if (Wanted("minimap-top-view")) yield return TopViewShot();
			DumpMenus();

			yield return Finish();
		}

		private static IEnumerator Finish()
		{
			foreach (var w in s_openedWindows.Where(w => w != null).ToList())
			{
				try { w.Close(); } catch (Exception e) { Log("close: " + e.Message); }
			}

			s_openedWindows.Clear();
			CloseToolWindows();
			yield return Wait(3);
			Restore();
			yield return Wait(2);
			try { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); } catch (Exception e) { Log("reopen scene: " + e.Message); }
			Log(s_failed ? "DocsCapture FAILED" : "DocsCapture done");
			if (s_exitWhenDone) EditorApplication.Exit(s_failed ? 1 : 0);
		}

		private static void Preflight()
		{
			Log($"unity {Application.unityVersion}, pixelsPerPoint {EditorGUIUtility.pixelsPerPoint}, proSkin {EditorGUIUtility.isProSkin}");
			if (!Mathf.Approximately(EditorGUIUtility.pixelsPerPoint, 1f)) Log("NOTE: pixelsPerPoint != 1; raw images are larger, annotate.py scales anchors by ppp.");
			if (!EditorGUIUtility.isProSkin) Log("WARNING: light editor skin; the style guide expects the dark skin.");
			var cfg = Config;
			Log($"config id {cfg.id}, guid {AssetDatabase.AssetPathToGUID(ConfigPath)}");
			bool inBuild = EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath);
			Log($"TestMap enabled in build scenes: {inBuild}");
		}

		private static MapMetaConfig Config => AssetDatabase.LoadAssetAtPath<MapMetaConfig>(ConfigPath);

		// ---------- snapshot / restore ----------

		private static void Snapshot()
		{
			s_fileSnapshot.Clear();
			foreach (var p in new[] { ConfigPath, ManagerConfigPath, TemplateConfigPath })
			{
				var full = Path.Combine(ProjectRoot, p);
				if (File.Exists(full)) s_fileSnapshot[p] = File.ReadAllBytes(full);
			}

			s_prefsSnapshot.Clear();
			foreach (var key in new[] { LanguagePreference, HdrpLitUiStateKey, BuildSummaryKey })
			{
				s_prefsSnapshot[key] = EditorPrefs.HasKey(key) ? PrefValue(key) : null;
			}

			s_gizmoSnapshot.Clear();
			foreach (var t in GizmoTypes)
			{
				if (GizmoUtility.TryGetGizmoInfo(t, out var info)) s_gizmoSnapshot[t] = info.iconEnabled;
			}
		}

		private static string BuildSummaryKey => "CarX.MapBuilder.BuildSummary." + Config.id;

		private static string PrefValue(string key)
		{
			// LanguagePreference and the summary are strings, the HDRP UI state is an int.
			return key == HdrpLitUiStateKey ? "i:" + EditorPrefs.GetInt(key) : "s:" + EditorPrefs.GetString(key);
		}

		private static Type[] GizmoTypes
		{
			get
			{
				var list = new List<Type> { typeof(Camera), typeof(Light), typeof(ReflectionProbe) };
				var hdProbe = Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalReflectionData, Unity.RenderPipelines.HighDefinition.Runtime");
				if (hdProbe != null) list.Add(hdProbe);
				return list.ToArray();
			}
		}

		private static void Restore()
		{
			foreach (var kv in s_fileSnapshot)
			{
				File.WriteAllBytes(Path.Combine(ProjectRoot, kv.Key), kv.Value);
				AssetDatabase.ImportAsset(kv.Key, ImportAssetOptions.ForceUpdate);
			}

			foreach (var kv in s_prefsSnapshot)
			{
				if (kv.Value == null) EditorPrefs.DeleteKey(kv.Key);
				else if (kv.Value.StartsWith("i:")) EditorPrefs.SetInt(kv.Key, int.Parse(kv.Value.Substring(2)));
				else EditorPrefs.SetString(kv.Key, kv.Value.Substring(2));
			}

			foreach (var kv in s_gizmoSnapshot) GizmoUtility.SetIconEnabled(kv.Key, kv.Value);

			var bakeDir = "Assets/MapResources/TestMap/Art/light_tower/RenderCore";
			if (AssetDatabase.IsValidFolder(bakeDir))
			{
				AssetDatabase.DeleteAsset(bakeDir);
				Log("deleted " + bakeDir);
			}

			try
			{
				var cache = Path.Combine(Application.temporaryCachePath, "StandaloneTemporary" + Config.id);
				if (Directory.Exists(cache)) Directory.Delete(cache, true);
			}
			catch (Exception e)
			{
				Log("cache cleanup: " + e.Message);
			}

			AssetDatabase.Refresh();
			Log("restored snapshot");
		}

		// ---------- demo data ----------

		private static void ApplyDemoMetadata()
		{
			var so = new SerializedObject(Config);
			so.FindProperty("mapMetaConfigValue.mapVersion").stringValue = "1.0.0";
			so.FindProperty("mapMetaConfigValue.summary").stringValue = "Compact test track for the MapUploader guide.";
			so.FindProperty("mapMetaConfigValue.mapDescription").stringValue =
				"A small sample map that ships with MapUploader. One spawn point; asphalt, concrete, metal and wood surfaces.";
			var authors = so.FindProperty("mapMetaConfigValue.authors");
			authors.arraySize = 1;
			authors.GetArrayElementAtIndex(0).stringValue = "CarX Technologies";
			authors.isExpanded = true;
			so.FindProperty("mapMetaConfigValue.url").stringValue = "https://github.com/CarXTechnologies/dro-map-uploader";
			so.ApplyModifiedPropertiesWithoutUndo();
			AssetDatabase.SaveAssetIfDirty(Config);
			Log("applied demo metadata");
		}

		// ---------- MapBuilder helpers ----------

		private static void CloseToolWindows()
		{
			foreach (var w in Resources.FindObjectsOfTypeAll(MapBuilderWindowType).Cast<EditorWindow>().ToList()) w.Close();
			global::Editor.Validation.MapValidationWindow.CloseIfOpen();
		}

		private static EditorWindow s_mb;

		private static IEnumerator OpenMapBuilder(string lang)
		{
			EditorPrefs.SetString(LanguagePreference, lang);
			foreach (var old in Resources.FindObjectsOfTypeAll(MapBuilderWindowType).Cast<EditorWindow>().ToList()) old.Close();
			yield return Wait(3);
			s_mb = (EditorWindow)ScriptableObject.CreateInstance(MapBuilderWindowType);
			s_mb.Show();
			s_mb.position = new Rect(60, 60, 1136, 760);
			s_openedWindows.Add(s_mb);
			yield return Wait(3);
			yield return WaitUntil(() => !(bool)Field(s_mb, "m_fetching") && !(bool)Call(s_mb, "IsDownloadAnyIcon"), 60, "MapBuilder fetch");
			Call(s_mb, "SelectLocalMap", Config);
			s_mb.Focus();
			yield return Settle(s_mb, 4);
		}

		private static object Field(object target, string name) => target.GetType().GetField(name, Instance)?.GetValue(target);
		private static void SetField(object target, string name, object value) => target.GetType().GetField(name, Instance).SetValue(target, value);

		private static object Call(object target, string name, params object[] args)
		{
			var method = target.GetType().GetMethod(name, Instance);
			if (method == null) throw new MissingMethodException(target.GetType().Name, name);
			return method.Invoke(target, args);
		}

		private static void Tab(int tab) => Call(s_mb, "SelectWorkspaceTab", tab);

		private static void Details(int details)
		{
			SetField(s_mb, "m_buildDetails", details);
			Call(s_mb, "RefreshBuildDetails");
		}

		private static void Destination(int value)
		{
			var enumType = MapBuilderWindowType.GetNestedType("PublishDestination", BindingFlags.NonPublic);
			SetField(s_mb, "m_publishDestination", Enum.ToObject(enumType, value));
			Call(s_mb, "RefreshDetailsPanel");
		}

		private static IEnumerator MapTabShots(string lang)
		{
			yield return OpenMapBuilder(lang);
			Tab(0);
			var mapPage = Field(s_mb, "m_mapPage") as ScrollView;
			if (mapPage != null) mapPage.scrollOffset = Vector2.zero;
			yield return Settle(s_mb, 5);
			ExpandAuthors();
			yield return Settle(s_mb, 3);
			Shoot(s_mb, "window-overview", lang);
			Shoot(s_mb, "map-settings", lang);
		}

		private static void ExpandAuthors()
		{
			foreach (var foldout in s_mb.rootVisualElement.Query<Foldout>().ToList())
			{
				if (foldout.GetFirstAncestorOfType<UnityEditor.UIElements.PropertyField>() is { } pf && pf.bindingPath == "mapMetaConfigValue.authors")
					foldout.value = true;
			}
		}

		private const string DemoPublicationTitle = "TestMap";

		private static IEnumerator LinkDemoPublication()
		{
			Tab(2);
			Destination(0);
			yield return WaitUntil(() => !(bool)Field(s_mb, "m_fetching") && !(bool)Call(s_mb, "IsDownloadAnyIcon"), 60, "publication list");
			yield return Settle(s_mb, 3);
			if (!global::Editor.MapBuilder.session.IsAuthenticated)
			{
				Log("not signed in: publish-modio will show the signed-out state");
				yield break;
			}

			var items = (System.Collections.IList)Field(s_mb, "m_fetchResultListItems");
			int index = -1;
			for (int i = 0; i < items.Count; i++)
			{
				if (items[i].GetType().GetProperty("Title")?.GetValue(items[i]) as string == DemoPublicationTitle) index = i;
			}

			Log($"publications: {items.Count}, demo index {index}");
			if (index >= 0) Call(s_mb, "OnItemRowClicked", index);
			yield return Settle(s_mb, 3);
		}

		// The account may own other publications (community maps); the published screenshot shows only the demo one.
		private static void KeepOnlyDemoPublication()
		{
			var items = (System.Collections.IList)Field(s_mb, "m_fetchResultListItems");
			for (int i = items.Count - 1; i >= 0; i--)
			{
				if (items[i].GetType().GetProperty("Title")?.GetValue(items[i]) as string != DemoPublicationTitle) items.RemoveAt(i);
			}

			SetField(s_mb, "m_selectItemIndex", items.Count > 0 ? 0 : -1);
			Call(s_mb, "RefreshItemsList");
		}

		private static IEnumerator BuildAndPublishShots(string lang)
		{
			yield return OpenMapBuilder(lang);
			yield return LinkDemoPublication();
			yield return BuildTestMap();
			yield return OpenMapBuilder(lang);

			Tab(1);
			Details(0);
			Call(s_mb, "RefreshDetailsPanel");
			yield return Settle(s_mb, 5);
			Shoot(s_mb, "build-tab", lang);

			Details(3);
			if (Field(s_mb, "m_buildPage") is ScrollView buildPage) buildPage.scrollOffset = Vector2.zero;
			yield return Settle(s_mb, 5);
			Shoot(s_mb, "build-optimization", lang);

			Details(0);
			Tab(2);
			SetField(s_mb, "m_pathToExternal", ExportFolderExample);
			Destination(2);
			yield return Settle(s_mb, 5);
			Shoot(s_mb, "export-folder", lang);

			Destination(0);
			yield return WaitUntil(() => !(bool)Field(s_mb, "m_fetching") && !(bool)Call(s_mb, "IsDownloadAnyIcon"), 60, "publication list");
			MapManagerConfig.GetPublishData(Config).changelog = lang == "ru" ? "Первый тестовый выпуск." : "First test release.";
			MapManagerConfig.instance.uploadName = false;
			MapManagerConfig.instance.uploadDescription = true;
			MapManagerConfig.instance.uploadPreview = true;
			KeepOnlyDemoPublication();
			Call(s_mb, "RefreshDetailsPanel");
			yield return Settle(s_mb, 5);
			Shoot(s_mb, "publish-modio", lang);
		}

		private static IEnumerator BuildTestMap()
		{
			Tab(1);
			SetField(s_mb, "m_buildFormat", FormatBuild.Binary);
			SetField(s_mb, "m_buildType", 3);
			Call(s_mb, "RefreshDetailsPanel");
			yield return Settle(s_mb, 2);
			Log("build started");
			Call(s_mb, "OnBuildButtonClicked");
			yield return Wait(10);
			yield return WaitUntil(() => !global::Editor.MapBuilder.IsBuilding && !(bool)Field(s_mb, "m_buildProcess"), 900, "build");
			global::Editor.Validation.MapValidationWindow.CloseIfOpen();
			yield return Wait(3);
			var build = MapManagerConfig.GetBuildOrEmpty(Config);
			Log($"build finished, buildSuccess = {build.buildSuccess}");
		}

		// ---------- validation ----------

		private static IEnumerator ValidationShot()
		{
			CloseToolWindows();
			yield return Wait(2);
			MapManagerConfig.instance.targetScene = ScenePath;
			// Collapse the long Geometry list so the Markers error is visible; the foldout state is a static dictionary.
			if (typeof(global::Editor.Validation.MapValidationWindow).GetField("s_foldouts", Static)?.GetValue(null) is Dictionary<string, bool> foldouts)
			{
				foldouts["Geometry"] = false;
				foldouts["Format"] = true;
				foldouts["Markers"] = true;
				foldouts["Lighting"] = true;
			}

			var go = new GameObject("DemoRoadWithoutCollider");
			var marker = go.AddComponent<GameMarkerData>();
			marker.markerData = new MarkerData { head = "Road", param = "Road/asphalt", lastHeadObject = "Road/asphalt" };
			global::Editor.MapBuilder.ValidateOnly(Config, FormatBuild.Binary);
			yield return Wait(5);
			var vw = Resources.FindObjectsOfTypeAll<global::Editor.Validation.MapValidationWindow>().FirstOrDefault();
			if (vw == null)
			{
				Log("validation window not found");
			}
			else
			{
				vw.position = new Rect(80, 80, 900, 700);
				vw.Focus();
				yield return Settle(vw, 5);
				Shoot(vw, "validation-window", "shared");
				vw.Close();
			}

			Object.DestroyImmediate(go);
			yield return Wait(2);
		}

		// ---------- inspectors ----------

		private static Type InspectorType => typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");

		private static IEnumerator OpenInspector(Object target, Func<Component, bool> expand, float height = 980)
		{
			if (target is GameObject go)
			{
				foreach (var c in go.GetComponents<Component>())
				{
					if (c != null) InternalEditorUtility.SetIsInspectorExpanded(c, expand(c));
				}
			}

			Selection.activeObject = target;
			yield return Wait(2);
			var inspector = (EditorWindow)ScriptableObject.CreateInstance(InspectorType);
			inspector.Show();
			inspector.position = new Rect(100, 40, 460, height);
			s_openedWindows.Add(inspector);
			yield return Wait(2);
			InspectorType.GetProperty("isLocked", Instance)?.SetValue(inspector, true);
			ActiveEditorTracker.sharedTracker.ForceRebuild();
			yield return Settle(inspector, 6);
			s_inspector = inspector;
		}

		private static EditorWindow s_inspector;

		private static IEnumerator CloseInspector()
		{
			if (s_inspector != null)
			{
				InspectorType.GetProperty("isLocked", Instance)?.SetValue(s_inspector, false);
				s_inspector.Close();
			}

			s_inspector = null;
			Selection.activeObject = null;
			yield return Wait(2);
		}

		private static GameObject FindInScene(string path)
		{
			var parts = path.Split('/');
			var root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == parts[0]);
			if (root == null) return null;
			var t = root.transform;
			for (int i = 1; i < parts.Length && t != null; i++) t = t.Find(parts[i]);
			return t != null ? t.gameObject : null;
		}

		private static GameObject FindByGlobalId(string globalId)
		{
			return GlobalObjectId.TryParse(globalId, out var id) ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as GameObject : null;
		}

		private static IEnumerator MaterialShot()
		{
			EditorPrefs.SetInt(HdrpLitUiStateKey, (1 << 0) | (1 << 1) | (1 << 6));
			var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
			yield return OpenInspector(material, _ => true, 1040);
			yield return Settle(s_inspector, 6);
			Shoot(s_inspector, "scene-hdrp-lit-material", "shared");
			yield return CloseInspector();
		}

		// The template shots use a template config and a Road marker created in memory, so the images don't depend on
		// what the shipped template config or the TestMap markers contain.
		private static object RoadValues(string param) => MarkerData.paramObjectsEditor["Road"](param);

		private static GameMarkerTemplateConfig CreateDemoTemplateConfig()
		{
			var config = ScriptableObject.CreateInstance<GameMarkerTemplateConfig>();
			config.name = "GameMarkerTemplateConfig";
			config.presets = new ListGameMarkerTemplate
			{
				presets = new[]
				{
					new MarkerData { head = "Road", param = "Road/asphalt", value = RoadValues("Road/asphalt"), index = 1, lastHeadObject = "Road", templateName = "AsphaultDef" },
					// The drawers treat the last preset as the "Custom" sentinel, as in Assets/Resources/GameMarkerTemplateConfig.asset.
					new MarkerData { head = "Road", param = "Road/default", index = 1, lastHeadObject = "Road", templateName = "Custom" },
				},
				selectHead = 3,
				templateName = "WetCobbles",
			};
			return config;
		}

		private static IEnumerator RoadMarkerShot()
		{
			var source = FindByGlobalId("GlobalObjectId_V1-2-259b9e7bcf9201647b685277124309e3-8134195311772322374-0") ?? FindInScene("Content/workshop_map_main/road");
			var go = new GameObject("road") { isStatic = true };
			var collider = go.AddComponent<MeshCollider>();
			if (source != null && source.GetComponent<MeshCollider>() is { } sourceCollider) collider.sharedMesh = sourceCollider.sharedMesh;
			var config = CreateDemoTemplateConfig();
			var marker = go.AddComponent<GameMarkerData>();
			marker.markerData = new MarkerData
			{
				head = "Road", param = "Road/asphalt", value = RoadValues("Road/asphalt"), index = 1, lastHeadObject = "Road",
				templateName = "AsphaultDef", templateConfig = config,
			};
			var markerSo = new SerializedObject(marker);
			foreach (var path in new[] { "markerData.value", "markerData.customValue" })
			{
				var property = markerSo.FindProperty(path);
				if (property != null) property.isExpanded = true;
			}

			markerSo.ApplyModifiedPropertiesWithoutUndo();
			yield return OpenInspector(go, c => c is GameMarkerData);
			yield return Settle(s_inspector, 10);
			Shoot(s_inspector, "markers-road-template", "shared");
			yield return CloseInspector();
			Object.DestroyImmediate(go);
			Object.DestroyImmediate(config);
		}

		private static IEnumerator TemplateConfigShot()
		{
			var config = CreateDemoTemplateConfig();
			var so = new SerializedObject(config);
			var first = so.FindProperty("presets.presets.Array.data[0].value");
			if (first != null) first.isExpanded = true;
			so.ApplyModifiedPropertiesWithoutUndo();
			yield return OpenInspector(config, _ => true);
			yield return Settle(s_inspector, 8);
			Shoot(s_inspector, "markers-template-config", "shared");
			yield return CloseInspector();
			Object.DestroyImmediate(config);
		}

		private static IEnumerator MaterialMarkerShot()
		{
			var go = FindByGlobalId("GlobalObjectId_V1-2-259b9e7bcf9201647b685277124309e3-1414104811014883703-0") ?? FindInScene("Flora/light_tower");
			if (go == null)
			{
				Log("light_tower not found");
				yield break;
			}

			var marker = go.AddComponent<GameMarkerData>();
			marker.markerData = new MarkerData
			{
				head = "Material",
				param = "Material",
				lastHeadObject = "Material",
				value = global::Editor.RenderCoreMaterialMarkerSettingsDrawer.CreateSettings(marker),
			};
			var so = new SerializedObject(marker);
			var entry = so.FindProperty("markerData.value.overrides.Array.data[0]");
			if (entry != null) entry.isExpanded = true;
			so.ApplyModifiedPropertiesWithoutUndo();
			yield return OpenInspector(go, c => c is GameMarkerData);
			yield return Settle(s_inspector, 8);
			Shoot(s_inspector, "markers-material", "shared");
			yield return CloseInspector();
			Object.DestroyImmediate(marker);
		}

		private static IEnumerator MinimapShot()
		{
			var go = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == "Minimap");
			if (go == null)
			{
				Log("Minimap not found");
				yield break;
			}

			var so = new SerializedObject(go.GetComponent<Minimap>());
			var textures = so.FindProperty("m_textures");
			if (textures != null)
			{
				textures.isExpanded = true;
				if (textures.arraySize > 0) textures.GetArrayElementAtIndex(0).isExpanded = true;
			}

			so.ApplyModifiedPropertiesWithoutUndo();
			yield return OpenInspector(go, c => c is Transform || c is Minimap);
			yield return Settle(s_inspector, 6);
			Shoot(s_inspector, "minimap-component", "shared");
			yield return CloseInspector();
		}

		private static IEnumerator CaptureCameraShot()
		{
			var go = FindInScene("Screen/PreviewScreen");
			if (go == null)
			{
				Log("PreviewScreen not found");
				yield break;
			}

			yield return OpenInspector(go, c => c is CaptureCamera);
			yield return Settle(s_inspector, 6);
			Shoot(s_inspector, "minimap-capture-camera", "shared");
			yield return CloseInspector();
		}

		// ---------- scene view ----------

		private static IEnumerator TopViewShot()
		{
			var gizmos = GizmoUtility.GetGizmoInfo();
			var gizmoState = gizmos.Select(g => (g, g.gizmoEnabled, g.iconEnabled)).ToList();
			foreach (var g in gizmos)
			{
				bool keep = g.name == "DrawZoneBehaviour";
				g.gizmoEnabled = keep;
				g.iconEnabled = false;
				GizmoUtility.ApplyGizmoInfo(g);
			}

			// Sound zones use DrawZoneBehaviour too; hide every zone except the minimap box for the shot.
			var hiddenZones = new List<GameObject>();
			foreach (var zone in Object.FindObjectsByType<DrawZoneBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				if (zone.GetComponent<Minimap>() != null) continue;
				zone.gameObject.SetActive(false);
				hiddenZones.Add(zone.gameObject);
			}

			Selection.activeObject = null;
			var sv = ScriptableObject.CreateInstance<SceneView>();
			sv.Show();
			sv.position = new Rect(80, 80, 700, 700);
			s_openedWindows.Add(sv);
			sv.drawGizmos = true;
			sv.sceneLighting = false;
			try { sv.overlayCanvas.overlaysEnabled = false; } catch (Exception e) { Log("overlays: " + e.Message); }
			sv.LookAt(Vector3.zero, Quaternion.Euler(90f, 0f, 0f), 330f, true, true);
			yield return Settle(sv, 10);
			sv.Focus();
			yield return Settle(sv, 4);

			var minimap = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == "Minimap");
			var extra = new Dictionary<string, object>();
			if (minimap != null)
			{
				var cam = sv.camera;
				var size = minimap.GetComponent<Minimap>() != null ? minimap.transform.localScale : Vector3.one * 600;
				float y = minimap.transform.position.y;
				extra["cameraPixelRect"] = cam.pixelRect;
				extra["corner"] = cam.WorldToScreenPoint(new Vector3(size.x / 2, y, size.z / 2));
				extra["center"] = cam.WorldToScreenPoint(new Vector3(0, y, 0));
				extra["topMid"] = cam.WorldToScreenPoint(new Vector3(0, y, size.z / 2));
			}

			Shoot(sv, "minimap-top-view", "shared", extra);
			sv.Close();
			foreach (var zone in hiddenZones) zone.SetActive(true);
			foreach (var (g, gizmo, icon) in gizmoState)
			{
				g.gizmoEnabled = gizmo;
				g.iconEnabled = icon;
				GizmoUtility.ApplyGizmoInfo(g);
			}

			yield return Wait(3);
		}

		// ---------- menus ----------

		private static void DumpMenus()
		{
			var sb = new StringBuilder();
			foreach (var root in new[] { "Assets/Create", "File", "GameObject", "Tools", "Edit" })
			{
				try
				{
					foreach (var item in Unsupported.GetSubmenus(root)) sb.AppendLine(item);
				}
				catch (Exception e)
				{
					sb.AppendLine($"# {root}: {e.Message}");
				}
			}

			Directory.CreateDirectory(s_outDir);
			File.WriteAllText(Path.Combine(s_outDir, "unity-menus.txt"), sb.ToString());
			Log("dumped menus");
		}

		// ---------- capture ----------

		private const string DemoAccount = "MapAuthor";

		// Signed-in shots must not show a personal account: replace only the displayed user name (style guide 11.2).
		private static void AnonymizeAccount(EditorWindow w)
		{
			foreach (var label in w.rootVisualElement.Query<Label>(className: "mb-auth-state").ToList())
			{
				int colon = label.text.IndexOf(':');
				if (colon > 0 && colon + 2 <= label.text.Length && !label.text.EndsWith(DemoAccount)) label.text = label.text.Substring(0, colon + 2) + DemoAccount;
			}
		}

		private static void Shoot(EditorWindow w, string id, string lang, Dictionary<string, object> extra = null)
		{
			if (!Wanted(id)) return;
			try
			{
				if (w.GetType() == MapBuilderWindowType)
				{
					AnonymizeAccount(w);
					w.rootVisualElement.MarkDirtyRepaint();
				}

				w.Repaint();
				var tex = Grab(w, out var viewRect);
				var dir = Path.Combine(s_outDir, lang);
				Directory.CreateDirectory(dir);
				File.WriteAllBytes(Path.Combine(dir, id + ".png"), tex.EncodeToPNG());
				File.WriteAllText(Path.Combine(dir, id + ".json"), DescribeLayout(w, tex, viewRect, extra));
				Object.DestroyImmediate(tex);
				Log($"shot {lang}/{id}");
			}
			catch (Exception e)
			{
				Log($"shot {lang}/{id} failed: {e}");
			}
		}

		private static Texture2D Grab(EditorWindow w, out Rect viewRect)
		{
			var parent = typeof(EditorWindow).GetField("m_Parent", Instance)!.GetValue(w);
			var guiView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GUIView");
			viewRect = (Rect)guiView.GetProperty("position", Instance)!.GetValue(parent);
			float ppp = EditorGUIUtility.pixelsPerPoint;
			int width = Mathf.RoundToInt(viewRect.width * ppp);
			int height = Mathf.RoundToInt(viewRect.height * ppp);
			var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			rt.Create();
			guiView.GetMethod("GrabPixels", Instance)!.Invoke(parent, new object[] { rt, new Rect(0, 0, width, height) });
			var previous = RenderTexture.active;
			RenderTexture.active = rt;
			var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
			tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			tex.Apply();
			RenderTexture.active = previous;
			rt.Release();
			Object.DestroyImmediate(rt);

			// GrabPixels returns the image bottom-up.
			var pixels = tex.GetPixels32();
			var flipped = new Color32[pixels.Length];
			for (int y = 0; y < height; y++) Array.Copy(pixels, y * width, flipped, (height - 1 - y) * width, width);
			for (int i = 0; i < flipped.Length; i++) flipped[i].a = 255;
			tex.SetPixels32(flipped);
			tex.Apply();
			return tex;
		}

		private static string DescribeLayout(EditorWindow w, Texture2D tex, Rect viewRect, Dictionary<string, object> extra)
		{
			float ppp = EditorGUIUtility.pixelsPerPoint;
			var root = w.rootVisualElement;
			// worldBound is relative to the grabbed host view, so it already includes the dock tab strip above the content.
			float contentOffsetY = root.worldBound.y;
			var sb = new StringBuilder();
			sb.Append('{');
			sb.Append($"\"window\":\"{w.GetType().FullName}\",\"ppp\":{F(ppp)},\"imageWidth\":{tex.width},\"imageHeight\":{tex.height},");
			sb.Append($"\"windowPosition\":{R(w.position)},\"viewRect\":{R(viewRect)},\"rootBound\":{R(root.worldBound)},\"contentOffsetY\":{F(contentOffsetY)},");
			if (extra != null)
			{
				sb.Append("\"extra\":{");
				sb.Append(string.Join(",", extra.Select(kv => $"\"{kv.Key}\":{Json(kv.Value)}")));
				sb.Append("},");
			}

			sb.Append("\"elements\":[");
			bool first = true;
			foreach (var e in Visible(root))
			{
				var r = e.worldBound;
				if (r.width < 1 || r.height < 1) continue;
				string text = e switch
				{
					Button b => b.text,
					Label l => l.text,
					TextField tf => tf.label + "|" + tf.value,
					Foldout f => f.text,
					_ => (e.GetType().GetProperty("label")?.GetValue(e) as string) ?? string.Empty,
				};
				string binding = (e as IBindable)?.bindingPath ?? string.Empty;
				string classes = string.Join(" ", e.GetClasses());
				if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(binding) && string.IsNullOrEmpty(e.name) && !classes.Contains("mb-")) continue;
				if (!first) sb.Append(',');
				first = false;
				sb.Append('{');
				sb.Append($"\"type\":\"{Esc(e.GetType().Name)}\",\"name\":\"{Esc(e.name)}\",\"classes\":\"{Esc(classes)}\",\"text\":\"{Esc(text)}\",\"binding\":\"{Esc(binding)}\",");
				sb.Append($"\"rect\":[{F(r.x * ppp)},{F(r.y * ppp)},{F(r.width * ppp)},{F(r.height * ppp)}],");
				sb.Append($"\"enabled\":{(e.enabledInHierarchy ? "true" : "false")}");
				sb.Append('}');
			}

			sb.Append("]}");
			return sb.ToString();
		}

		private static IEnumerable<VisualElement> Visible(VisualElement root)
		{
			var stack = new Stack<VisualElement>();
			stack.Push(root);
			while (stack.Count > 0)
			{
				var e = stack.Pop();
				if (e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.visibility == Visibility.Hidden) continue;
				yield return e;
				for (int i = e.hierarchy.childCount - 1; i >= 0; i--) stack.Push(e.hierarchy[i]);
			}
		}

		private static string F(float v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
		private static string R(Rect r) => $"[{F(r.x)},{F(r.y)},{F(r.width)},{F(r.height)}]";

		private static string Json(object v) => v switch
		{
			Rect r => R(r),
			Vector3 p => $"[{F(p.x)},{F(p.y)},{F(p.z)}]",
			Vector2 p => $"[{F(p.x)},{F(p.y)}]",
			float f => F(f),
			_ => $"\"{Esc(v?.ToString())}\"",
		};

		private static string Esc(string s)
		{
			if (string.IsNullOrEmpty(s)) return string.Empty;
			var sb = new StringBuilder(s.Length);
			foreach (char c in s)
			{
				switch (c)
				{
					case '"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': break;
					case '\t': sb.Append("\\t"); break;
					default:
						if (c < 0x20) sb.Append(' ');
						else sb.Append(c);
						break;
				}
			}

			return sb.ToString();
		}
	}
}
