using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.Validation
{
	public sealed class MapValidationWindow : EditorWindow
	{
		private static readonly Dictionary<string, bool> s_foldouts = new();

		private MapValidationReport m_report;
		private Action m_revalidate;
		private Vector2 m_scroll;

		private bool m_showErrors = true;
		private bool m_showWarnings = true;
		private bool m_showInfo = true;
		private string m_search = string.Empty;

		public static void Show(MapValidationReport report, Action revalidate = null)
		{
			if (report == null)
			{
				return;
			}

			var window = GetWindow<MapValidationWindow>(utility: false, title: "Map Validation", focus: true);
			window.minSize = new Vector2(640, 300);
			window.m_report = report;
			window.m_revalidate = revalidate;
			window.m_scroll = Vector2.zero;
			window.Repaint();
		}

		public static void CloseIfOpen()
		{
			if (HasOpenInstances<MapValidationWindow>())
			{
				GetWindow<MapValidationWindow>(utility: false, title: "Map Validation", focus: false).Close();
			}
		}

		private void OnGUI()
		{
			if (m_report == null)
			{
				EditorGUILayout.HelpBox("Nothing has been validated yet.", MessageType.Info);
				return;
			}

			DrawHeader();
			DrawToolbar();
			DrawIssues();
		}

		private void DrawHeader()
		{
			var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField($"{m_report.title}  -  {m_report.sceneName}  ({m_report.format})", style);

			var messageType = m_report.HasErrors
				? MessageType.Error
				: m_report.WarningCount > 0
					? MessageType.Warning
					: MessageType.Info;

			EditorGUILayout.HelpBox(
				m_report.IsEmpty
					? "No problems found. The map is ready to build."
					: m_report.Headline() + (m_report.HasErrors ? " - the build cannot proceed until the errors are fixed." : string.Empty),
				messageType);
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				m_showErrors = GUILayout.Toggle(m_showErrors, $"Errors ({m_report.ErrorCount})", EditorStyles.toolbarButton, GUILayout.Width(90));
				m_showWarnings = GUILayout.Toggle(m_showWarnings, $"Warnings ({m_report.WarningCount})", EditorStyles.toolbarButton, GUILayout.Width(110));
				m_showInfo = GUILayout.Toggle(m_showInfo, "Notes", EditorStyles.toolbarButton, GUILayout.Width(60));

				GUILayout.Space(8);
				m_search = GUILayout.TextField(m_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));

				GUILayout.FlexibleSpace();

				if (m_revalidate != null && GUILayout.Button("Re-run", EditorStyles.toolbarButton, GUILayout.Width(60)))
				{
					m_revalidate();
					GUIUtility.ExitGUI();
				}

				if (GUILayout.Button(new GUIContent("To console", "Log every issue to the console, each linked to its object"),
					    EditorStyles.toolbarButton, GUILayout.Width(80)))
				{
					m_report.WriteToConsole();
				}

				if (GUILayout.Button("Copy", EditorStyles.toolbarButton, GUILayout.Width(50)))
				{
					EditorGUIUtility.systemCopyBuffer = m_report.ToPlainText();
					ShowNotification(new GUIContent("Report copied"));
				}
			}
		}

		private void DrawIssues()
		{
			var visible = m_report.Issues.Where(Passes).ToList();

			if (visible.Count == 0)
			{
				EditorGUILayout.Space(8);
				EditorGUILayout.LabelField(m_report.IsEmpty ? "Nothing to show." : "Nothing matches the current filter.",
					EditorStyles.centeredGreyMiniLabel);
				return;
			}

			m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

			foreach (var group in visible.GroupBy(issue => issue.category).OrderBy(group => group.Key))
			{
				s_foldouts.TryGetValue(group.Key, out var expanded);

				if (!s_foldouts.ContainsKey(group.Key))
				{
					expanded = true;
				}

				expanded = EditorGUILayout.Foldout(expanded, $"{group.Key}  ({group.Count()})", true, EditorStyles.foldoutHeader);
				s_foldouts[group.Key] = expanded;

				if (!expanded)
				{
					continue;
				}

				EditorGUI.indentLevel++;

				foreach (var issue in group)
				{
					DrawIssue(issue);
				}

				EditorGUI.indentLevel--;
				EditorGUILayout.Space(2);
			}

			EditorGUILayout.EndScrollView();
		}

		private void DrawIssue(MapValidationIssue issue)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				var icon = issue.severity switch
				{
					MapValidationSeverity.Error => EditorGUIUtility.IconContent("console.erroricon.sml"),
					MapValidationSeverity.Warning => EditorGUIUtility.IconContent("console.warnicon.sml"),
					_ => EditorGUIUtility.IconContent("console.infoicon.sml"),
				};

				GUILayout.Label(icon, GUILayout.Width(24), GUILayout.Height(18));

				var label = new GUIStyle(EditorStyles.label) { wordWrap = true, richText = false };
				EditorGUILayout.LabelField(issue.message, label);

				if (!issue.HasObject)
				{
					GUILayout.Space(72);
					return;
				}

				if (GUILayout.Button(new GUIContent("Select", issue.objectPath), EditorStyles.miniButton, GUILayout.Width(64)))
				{
					var target = issue.ResolveObject();

					if (target == null)
					{
						ShowNotification(new GUIContent($"'{issue.objectPath}' is not in the open scene"));
						return;
					}

					Selection.activeObject = target;
					EditorGUIUtility.PingObject(target);
				}
			}
		}

		private bool Passes(MapValidationIssue issue)
		{
			var severityMatches = issue.severity switch
			{
				MapValidationSeverity.Error => m_showErrors,
				MapValidationSeverity.Warning => m_showWarnings,
				_ => m_showInfo,
			};

			if (!severityMatches)
			{
				return false;
			}

			if (string.IsNullOrWhiteSpace(m_search))
			{
				return true;
			}

			return issue.message.IndexOf(m_search, StringComparison.OrdinalIgnoreCase) >= 0
			       || issue.category.IndexOf(m_search, StringComparison.OrdinalIgnoreCase) >= 0
			       || (issue.objectPath ?? string.Empty).IndexOf(m_search, StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}
