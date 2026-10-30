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
using ModItem = Plugins.CarX.Modding.Creator.Runtime.Publishing.ModItem;
using Object = UnityEngine.Object;

namespace MapUploader.DocsCapture
{
	/// <summary>
	/// Maintainer-only tool that captures the raw screenshots for docs/images. It lives in docs/_capture/Editor and is copied
	/// into Assets/Editor/DocsCapture only while capturing (see docs/_capture/README.md). Each shot is saved as a raw PNG of
	/// the whole window plus a JSON file with the rectangles of every visible UI Toolkit element; docs/_capture/annotate.py
	/// crops, numbers and compresses them into docs/images.
	/// The run never saves a scene. It writes back the assets it changes and restores the EditorPrefs keys and gizmo settings
	/// it touches; docs/_capture/README.md lists what it doesn't restore.
	/// Private members of MapBuilderEditorWindow and MapValidationWindow are read by reflection; every lookup throws with the
	/// member name when the member is gone, so a renamed member fails the run instead of silently changing a shot.
	/// </summary>
	public static class DocsCapture
	{
		private const string ScenePath = "Assets/MapResources/TestMap/TestMap.unity";
		private const string ConfigPath = "Assets/MapResources/TestMap/TestMapConfig.asset";
		private const string ManagerConfigPath = "Assets/Resources/MapManagerConfig.asset";
		private const string TemplateConfigPath = "Assets/Resources/GameMarkerTemplateConfig.asset";
		private const string MaterialPath = "Assets/MapResources/TestMap/Art/0x4F368125.mat";
		private const string HdrpLitUiStateKey = "CoreRP:Material:UI_State:HDRP/Lit";
		private const string ExportFolderExample = "C:/Games/CarX Drift Racing Online 2/Mods/TestMap";
		private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
		private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

		// The shots RunAll takes, by raw folder. docs/_capture/shots.json describes the same ids.
		private static readonly string[] LanguageShots = { "window-overview", "map-settings", "build-tab", "build-optimization", "export-folder", "publish-modio" };
		private static readonly string[] SharedShots =
		{
			"validation-window", "scene-hdrp-lit-material", "markers-road-template", "markers-template-config", "markers-material",
			"minimap-component", "minimap-capture-camera", "minimap-top-view",
		};

		private static readonly Stack<IEnumerator> s_routines = new();
		private static bool s_exitWhenDone;
		private static bool s_failed;
		private static bool s_finishing;
		private static string s_outDir;
		private static StringBuilder s_log;
		private static readonly Dictionary<string, byte[]> s_fileSnapshot = new();
		private static readonly Dictionary<string, string> s_prefsSnapshot = new();
		private static readonly Dictionary<Type, bool> s_gizmoSnapshot = new();
		private static readonly List<EditorWindow> s_openedWindows = new();
		private static readonly HashSet<string> s_only = new();

		private static Type MapBuilderWindowType => typeof(global::Editor.MapBuilderEditorWindow);

		// MapBuilderEditorWindow.LanguagePreference is internal to MapUploader.Editor, so it is read by reflection.
		private static string LanguagePreference => (string)StaticField(MapBuilderWindowType, "LanguagePreference");

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
			s_finishing = false;
			// A snapshot left from an earlier run in this editor session must never be written back.
			s_fileSnapshot.Clear();
			s_prefsSnapshot.Clear();
			s_gizmoSnapshot.Clear();
			s_openedWindows.Clear();
			s_routines.Clear();
			s_routines.Push(routine);
			EditorApplication.update -= Tick;
			EditorApplication.update += Tick;
			Log($"DocsCapture started, out = {s_outDir}");
			foreach (var id in s_only.Where(id => !LanguageShots.Contains(id) && !SharedShots.Contains(id))) Fail("unknown shot id " + id);
			try
			{
				CleanOutput();
			}
			catch (Exception e)
			{
				Fail($"could not delete the raw shots of an earlier run in {s_outDir}: {e.Message}");
			}
		}

		// A full run deletes every raw shot; a run with -docsCaptureOnly deletes the raw shots of those ids. So annotate.py
		// never picks up a raw shot of an earlier run in place of one this run failed to take.
		private static void CleanOutput()
		{
			foreach (var folder in new[] { "en", "ru", "shared" })
			{
				var dir = Path.Combine(s_outDir, folder);
				if (!Directory.Exists(dir)) continue;
				foreach (var file in Directory.GetFiles(dir))
				{
					var name = Path.GetFileName(file);
					bool stale = s_only.Count == 0
						? name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
						: s_only.Any(id => name == id + ".png" || name == id + ".json" || name == id + ".debug.png");
					if (stale) File.Delete(file);
				}
			}
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
				DisposeRoutines();
				if (s_finishing)
				{
					// Finish itself threw: stop here instead of starting Finish again, which would loop.
					Log("DocsCapture FAILED while restoring the project; restore the files listed in docs/_capture/README.md by hand.");
					EditorApplication.update -= Tick;
					if (s_exitWhenDone) EditorApplication.Exit(1);
					return;
				}

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

		// Disposing the suspended routines runs their finally blocks, which put back what they changed (see TopViewShot).
		private static void DisposeRoutines()
		{
			while (s_routines.Count > 0)
			{
				var routine = s_routines.Pop();
				try
				{
					(routine as IDisposable)?.Dispose();
				}
				catch (Exception e)
				{
					Log("EXCEPTION while cleaning up: " + e);
				}
			}
		}

		private static void Fail(string message)
		{
			s_failed = true;
			Log("FAILED: " + message);
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
					Fail($"timeout after {timeoutSeconds} s waiting for {what}");
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

			// The Map tab shots of both languages come before the first build and the first publication link, so that the
			// map row in window-overview shows the same build and mod.io state in English and Russian.
			foreach (var lang in new[] { "en", "ru" })
			{
				if (Wanted("window-overview") || Wanted("map-settings")) yield return MapTabShots(lang);
			}

			foreach (var lang in new[] { "en", "ru" })
			{
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
			s_finishing = true;
			foreach (var w in s_openedWindows.Where(w => w != null).ToList())
			{
				try { w.Close(); } catch (Exception e) { Log("close: " + e.Message); }
			}

			s_openedWindows.Clear();
			Try("close tool windows", CloseToolWindows);
			yield return Wait(3);
			Restore();
			yield return Wait(2);
			try { EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); } catch (Exception e) { Log("reopen scene: " + e.Message); }
			CheckRawShots();
			Log(s_failed ? "DocsCapture FAILED" : "DocsCapture done");
			if (s_exitWhenDone) EditorApplication.Exit(s_failed ? 1 : 0);
		}

		// Every wanted shot must have its raw PNG and JSON; a shot that was skipped without an error still fails the run.
		private static void CheckRawShots()
		{
			foreach (var id in LanguageShots.Concat(SharedShots).Where(Wanted))
			{
				foreach (var folder in LanguageShots.Contains(id) ? new[] { "en", "ru" } : new[] { "shared" })
				{
					var raw = Path.Combine(s_outDir, folder, id);
					if (!File.Exists(raw + ".png") || !File.Exists(raw + ".json")) Fail($"no raw shot {folder}/{id}");
				}
			}
		}

		// Runs one restore step; a failure is logged and fails the run, and the remaining steps still run.
		private static void Try(string what, Action action)
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				Fail($"{what}: {e}");
			}
		}

		private static void Preflight()
		{
			float ppp = EditorGUIUtility.pixelsPerPoint;
			Log($"unity {Application.unityVersion}, pixelsPerPoint {ppp}, proSkin {EditorGUIUtility.isProSkin}");
			Log($"annotate.py multiplies the point values in shots.json (crop edges, pads, px anchors) by pixelsPerPoint {ppp}.");
			if (ppp < 1.75f) Log("NOTE: display scale below 175%; annotate.py never upscales, so some images come out smaller than at 175%.");
			if (!EditorGUIUtility.isProSkin) Log("WARNING: light editor skin; the style guide expects the dark skin.");
			var cfg = Config;
			if (cfg == null) throw new InvalidOperationException("no MapMetaConfig at " + ConfigPath);
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

		// The prefix is a literal in MapBuilderEditorWindow.Workspace.cs, where the build summary is saved; keep them equal.
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
				Try("restore " + kv.Key, () =>
				{
					File.WriteAllBytes(Path.Combine(ProjectRoot, kv.Key), kv.Value);
					AssetDatabase.ImportAsset(kv.Key, ImportAssetOptions.ForceUpdate);
				});
			}

			foreach (var kv in s_prefsSnapshot)
			{
				Try("restore EditorPrefs " + kv.Key, () =>
				{
					if (kv.Value == null) EditorPrefs.DeleteKey(kv.Key);
					else if (kv.Value.StartsWith("i:")) EditorPrefs.SetInt(kv.Key, int.Parse(kv.Value.Substring(2)));
					else EditorPrefs.SetString(kv.Key, kv.Value.Substring(2));
				});
			}

			foreach (var kv in s_gizmoSnapshot) Try("restore gizmo icon " + kv.Key.Name, () => GizmoUtility.SetIconEnabled(kv.Key, kv.Value));

			Try("delete the material bake folder", () =>
			{
				var bakeDir = "Assets/MapResources/TestMap/Art/light_tower/RenderCore";
				if (AssetDatabase.IsValidFolder(bakeDir))
				{
					AssetDatabase.DeleteAsset(bakeDir);
					Log("deleted " + bakeDir);
				}
			});

			try
			{
				var cache = Path.Combine(Application.temporaryCachePath, "StandaloneTemporary" + Config.id);
				if (Directory.Exists(cache)) Directory.Delete(cache, true);
			}
			catch (Exception e)
			{
				Log("cache cleanup: " + e.Message);
			}

			Try("refresh the asset database", () => AssetDatabase.Refresh());
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
			yield return WaitUntil(IsMapBuilderIdle, 60, "MapBuilder fetch");
			Call(s_mb, "SelectLocalMap", Config);
			s_mb.Focus();
			yield return Settle(s_mb, 4);
		}

		private static bool IsMapBuilderIdle() => !Field<bool>(s_mb, "m_fetching") && !Call<bool>(s_mb, "IsDownloadAnyIcon");

		// ---------- reflection ----------
		// Every lookup throws with the type and member name when the member is gone or has another type.

		private static FieldInfo FindField(Type type, string name, BindingFlags flags) =>
			type.GetField(name, flags) ?? throw new MissingFieldException(type.FullName, name);

		private static PropertyInfo FindProperty(Type type, string name) =>
			type.GetProperty(name, Instance) ?? throw new MissingMemberException(type.FullName, name);

		private static MethodInfo FindMethod(Type type, string name) =>
			type.GetMethod(name, Instance) ?? throw new MissingMethodException(type.FullName, name);

		private static Type FindType(Assembly assembly, string fullName) =>
			assembly.GetType(fullName) ?? throw new TypeLoadException($"type {fullName} not found in {assembly.GetName().Name}");

		private static T As<T>(object value, Type owner, string name)
		{
			if (value is T typed) return typed;
			throw new InvalidCastException($"{owner.FullName}.{name} is {value?.GetType().FullName ?? "null"}, expected {typeof(T).FullName}");
		}

		private static object Field(object target, string name) => FindField(target.GetType(), name, Instance).GetValue(target);
		private static T Field<T>(object target, string name) => As<T>(Field(target, name), target.GetType(), name);
		private static object StaticField(Type type, string name) => FindField(type, name, Static).GetValue(null);
		private static void SetField(object target, string name, object value) => FindField(target.GetType(), name, Instance).SetValue(target, value);

		private static object Call(object target, string name, params object[] args) => FindMethod(target.GetType(), name).Invoke(target, args);
		private static T Call<T>(object target, string name, params object[] args) => As<T>(Call(target, name, args), target.GetType(), name);

		private static void Tab(int tab) => Call(s_mb, "SelectWorkspaceTab", tab);

		private static void Details(int details)
		{
			SetField(s_mb, "m_buildDetails", details);
			Call(s_mb, "RefreshBuildDetails");
		}

		private static void Destination(int value)
		{
			var enumType = MapBuilderWindowType.GetNestedType("PublishDestination", BindingFlags.NonPublic)
				?? throw new TypeLoadException($"nested type {MapBuilderWindowType.FullName}.PublishDestination not found");
			SetField(s_mb, "m_publishDestination", Enum.ToObject(enumType, value));
			Call(s_mb, "RefreshDetailsPanel");
		}

		private static IEnumerator MapTabShots(string lang)
		{
			yield return OpenMapBuilder(lang);
			Tab(0);
			Field<ScrollView>(s_mb, "m_mapPage").scrollOffset = Vector2.zero;
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
			yield return WaitUntil(IsMapBuilderIdle, 60, "publication list");
			yield return Settle(s_mb, 3);
			if (!global::Editor.MapBuilder.session.IsAuthenticated)
			{
				// Not a failure: the other shots don't depend on the sign-in state. annotate.py stops on publish-modio, whose
				// callouts describe the signed-in state (docs/_capture/README.md, Sign-in state).
				if (Wanted("publish-modio")) Log("WARNING: not signed in to mod.io; publish-modio shows the signed-out state, which shots.json doesn't describe");
				yield break;
			}

			var items = Field<List<ModItem>>(s_mb, "m_fetchResultListItems");
			int index = items.FindIndex(item => item.Title == DemoPublicationTitle);
			Log($"publications: {items.Count}, demo index {index}");
			if (index >= 0) Call(s_mb, "OnItemRowClicked", index);
			else if (Wanted("publish-modio")) Fail($"the signed-in account has no publication titled {DemoPublicationTitle}");
			yield return Settle(s_mb, 3);
		}

		// The account may own other publications (community maps); the published screenshot shows only the demo one.
		private static void KeepOnlyDemoPublication()
		{
			var items = Field<List<ModItem>>(s_mb, "m_fetchResultListItems");
			items.RemoveAll(item => item.Title != DemoPublicationTitle);
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
			Field<ScrollView>(s_mb, "m_buildPage").scrollOffset = Vector2.zero;
			yield return Settle(s_mb, 5);
			Shoot(s_mb, "build-optimization", lang);

			Details(0);
			Tab(2);
			SetField(s_mb, "m_pathToExternal", ExportFolderExample);
			Destination(2);
			yield return Settle(s_mb, 5);
			Shoot(s_mb, "export-folder", lang);

			Destination(0);
			yield return WaitUntil(IsMapBuilderIdle, 60, "publication list");
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
			yield return WaitUntil(() => !global::Editor.MapBuilder.IsBuilding && !Field<bool>(s_mb, "m_buildProcess"), 900, "build");
			global::Editor.Validation.MapValidationWindow.CloseIfOpen();
			yield return Wait(3);
			var build = MapManagerConfig.GetBuildOrEmpty(Config);
			Log($"build finished, buildSuccess = {build.buildSuccess}");
			// 3 = TempData.Map | TempData.Meta: both parts built. Anything less means the build shots show a failed build.
			if (build.buildSuccess != (int)(TempData.Map | TempData.Meta)) Fail($"TestMap did not build, buildSuccess = {build.buildSuccess}");
		}

		// ---------- validation ----------

		private static IEnumerator ValidationShot()
		{
			CloseToolWindows();
			yield return Wait(2);
			MapManagerConfig.instance.targetScene = ScenePath;
			// Collapse the long Geometry list so the Markers error is visible; the foldout state is a static dictionary.
			var windowType = typeof(global::Editor.Validation.MapValidationWindow);
			var foldouts = As<Dictionary<string, bool>>(StaticField(windowType, "s_foldouts"), windowType, "s_foldouts");
			foldouts["Geometry"] = false;
			foldouts["Format"] = true;
			foldouts["Markers"] = true;
			foldouts["Lighting"] = true;

			var go = new GameObject("DemoRoadWithoutCollider");
			var marker = go.AddComponent<GameMarkerData>();
			marker.markerData = new MarkerData { head = "Road", param = "Road/asphalt", lastHeadObject = "Road/asphalt" };
			global::Editor.MapBuilder.ValidateOnly(Config, FormatBuild.Binary);
			yield return Wait(5);
			var vw = Resources.FindObjectsOfTypeAll<global::Editor.Validation.MapValidationWindow>().FirstOrDefault();
			if (vw == null)
			{
				Fail("the Map Validation window did not open");
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

		private static Type InspectorType => FindType(typeof(UnityEditor.Editor).Assembly, "UnityEditor.InspectorWindow");

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
			FindProperty(InspectorType, "isLocked").SetValue(inspector, true);
			ActiveEditorTracker.sharedTracker.ForceRebuild();
			yield return Settle(inspector, 6);
			s_inspector = inspector;
		}

		private static EditorWindow s_inspector;

		private static IEnumerator CloseInspector()
		{
			if (s_inspector != null)
			{
				FindProperty(InspectorType, "isLocked").SetValue(s_inspector, false);
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
			if (material == null)
			{
				Fail("material not found: " + MaterialPath);
				yield break;
			}

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
				Fail("Flora/light_tower not found in the TestMap scene");
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
				Fail("the Minimap root object not found in the TestMap scene");
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
				Fail("Screen/PreviewScreen not found in the TestMap scene");
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
			var hiddenZones = new List<GameObject>();
			SceneView sv = null;
			// The finally block also runs when the run stops on an exception: Tick disposes the suspended routines.
			try
			{
				foreach (var g in gizmos)
				{
					bool keep = g.name == "DrawZoneBehaviour";
					g.gizmoEnabled = keep;
					g.iconEnabled = false;
					GizmoUtility.ApplyGizmoInfo(g);
				}

				// Sound zones use DrawZoneBehaviour too; hide every zone except the minimap box for the shot.
				foreach (var zone in Object.FindObjectsByType<DrawZoneBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
				{
					if (zone.GetComponent<Minimap>() != null) continue;
					zone.gameObject.SetActive(false);
					hiddenZones.Add(zone.gameObject);
				}

				Selection.activeObject = null;
				sv = ScriptableObject.CreateInstance<SceneView>();
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
			}
			finally
			{
				if (sv != null) sv.Close();
				foreach (var zone in hiddenZones)
				{
					if (zone != null) zone.SetActive(true);
				}

				foreach (var (g, gizmo, icon) in gizmoState)
				{
					g.gizmoEnabled = gizmo;
					g.iconEnabled = icon;
					GizmoUtility.ApplyGizmoInfo(g);
				}
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

		// Signed-in shots must not show a personal account: replace only the displayed user name (docs/CONTRIBUTING.md, Files, names and sizes).
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
				// Delete the raw shot of an earlier run first, so a failed shot never leaves an old one for annotate.py.
				// File.Delete throws only when the folder is missing, so create it first.
				var dir = Path.Combine(s_outDir, lang);
				Directory.CreateDirectory(dir);
				File.Delete(Path.Combine(dir, id + ".png"));
				File.Delete(Path.Combine(dir, id + ".json"));
				if (w.GetType() == MapBuilderWindowType)
				{
					AnonymizeAccount(w);
					w.rootVisualElement.MarkDirtyRepaint();
				}

				w.Repaint();
				var tex = Grab(w, out var viewRect);
				File.WriteAllBytes(Path.Combine(dir, id + ".png"), tex.EncodeToPNG());
				File.WriteAllText(Path.Combine(dir, id + ".json"), DescribeLayout(w, tex, viewRect, extra));
				Object.DestroyImmediate(tex);
				Log($"shot {lang}/{id}");
			}
			catch (Exception e)
			{
				Fail($"shot {lang}/{id}: {e}");
			}
		}

		private static Texture2D Grab(EditorWindow w, out Rect viewRect)
		{
			var parent = FindField(typeof(EditorWindow), "m_Parent", Instance).GetValue(w)
				?? throw new InvalidOperationException($"{w.GetType().Name} has no host view; it isn't shown");
			var guiView = FindType(typeof(EditorWindow).Assembly, "UnityEditor.GUIView");
			viewRect = As<Rect>(FindProperty(guiView, "position").GetValue(parent), guiView, "position");
			float ppp = EditorGUIUtility.pixelsPerPoint;
			int width = Mathf.RoundToInt(viewRect.width * ppp);
			int height = Mathf.RoundToInt(viewRect.height * ppp);
			var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			rt.Create();
			FindMethod(guiView, "GrabPixels").Invoke(parent, new object[] { rt, new Rect(0, 0, width, height) });
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
