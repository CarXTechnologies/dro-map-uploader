using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Editor.Validation
{
	public sealed class MapValidationReport
	{
		private readonly List<MapValidationIssue> m_issues = new();
		private readonly Dictionary<string, int> m_emitted = new();

		public string title = string.Empty;
		public string sceneName = string.Empty;
		public FormatBuild format;
		public Transform pathRoot;

		public IReadOnlyList<MapValidationIssue> Issues => m_issues;

		public int ErrorCount { get; private set; }

		public int WarningCount { get; private set; }

		public bool HasErrors => ErrorCount > 0;

		public bool IsEmpty => m_issues.Count == 0;

		public void Clear()
		{
			m_issues.Clear();
			m_emitted.Clear();
			ErrorCount = 0;
			WarningCount = 0;
		}

		public void Error(string category, string message, Object context = null)
		{
			Add(MapValidationSeverity.Error, category, message, context);
		}

		public void Warning(string category, string message, Object context = null)
		{
			Add(MapValidationSeverity.Warning, category, message, context);
		}

		public void Info(string category, string message, Object context = null)
		{
			Add(MapValidationSeverity.Info, category, message, context);
		}

		public void Add(MapValidationSeverity severity, string category, string message, Object context = null)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				return;
			}

			var issue = new MapValidationIssue
			{
				severity = severity,
				category = string.IsNullOrEmpty(category) ? "General" : category,
				message = message,
			};

			if (context != null)
			{
				issue.instanceId = context.GetInstanceID();

				var transform = ResolveTransform(context);

				if (transform != null)
				{
					issue.objectPath = BuildPath(transform, pathRoot);
				}
			}

			m_issues.Add(issue);

			switch (severity)
			{
				case MapValidationSeverity.Error:
					ErrorCount++;
					break;
				case MapValidationSeverity.Warning:
					WarningCount++;
					break;
			}
		}

		public void AddCapped(MapValidationSeverity severity, string category, string rule, string message,
			Object context = null, int limit = 25)
		{
			m_emitted.TryGetValue(rule, out var seen);
			m_emitted[rule] = seen + 1;

			if (seen < limit)
			{
				Add(severity, category, message, context);
			}
		}

		public void FlushSuppressed(int limit = 25)
		{
			foreach (var pair in m_emitted)
			{
				if (pair.Value <= limit)
				{
					continue;
				}

				Add(MapValidationSeverity.Info, "Summary",
					$"{pair.Key}: {pair.Value - limit} more of the same, not listed individually.");
			}

			m_emitted.Clear();
		}

		public string Headline()
		{
			if (IsEmpty)
			{
				return "No problems found.";
			}

			var infos = m_issues.Count - ErrorCount - WarningCount;
			var parts = new List<string>();

			if (ErrorCount > 0)
			{
				parts.Add(ErrorCount == 1 ? "1 error" : $"{ErrorCount} errors");
			}

			if (WarningCount > 0)
			{
				parts.Add(WarningCount == 1 ? "1 warning" : $"{WarningCount} warnings");
			}

			if (infos > 0)
			{
				parts.Add(infos == 1 ? "1 note" : $"{infos} notes");
			}

			return string.Join(", ", parts);
		}

		public void WriteSummaryToConsole()
		{
			var where = string.IsNullOrEmpty(sceneName) ? title : $"{title} ({sceneName})";

			if (IsEmpty)
			{
				Debug.Log($"Map validation - {where}: no problems found.");
				return;
			}

			var text = $"Map validation - {where}: {Headline()}. The Map Validation window has the list.";

			if (HasErrors)
			{
				Debug.LogError(text);
			}
			else
			{
				Debug.LogWarning(text);
			}
		}

		public void WriteToConsole()
		{
			foreach (var issue in m_issues)
			{
				var context = issue.ResolveObject();
				var text = $"[Map validation] {issue.category}: {issue.message}";

				switch (issue.severity)
				{
					case MapValidationSeverity.Error:
						Debug.LogError(text, context);
						break;
					case MapValidationSeverity.Warning:
						Debug.LogWarning(text, context);
						break;
					default:
						Debug.Log(text, context);
						break;
				}
			}
		}

		public string ToPlainText()
		{
			var builder = new StringBuilder();

			builder.AppendLine($"Map validation - {title} ({sceneName}, {format})");
			builder.AppendLine(Headline());
			builder.AppendLine();

			foreach (var issue in m_issues)
			{
				builder.Append(issue.severity.ToString().ToUpperInvariant())
					.Append(" [").Append(issue.category).Append("] ")
					.Append(issue.message);

				if (!string.IsNullOrEmpty(issue.objectPath))
				{
					builder.Append("  <- ").Append(issue.objectPath);
				}

				builder.AppendLine();
			}

			return builder.ToString();
		}

		private static Transform ResolveTransform(Object context)
		{
			switch (context)
			{
				case null:
					return null;
				case GameObject gameObject:
					return gameObject.transform;
				case Component component:
					return component.transform;
				default:
					return null;
			}
		}

		private static string BuildPath(Transform target, Transform stripRoot)
		{
			var segments = new List<string>();

			for (var current = target; current != null && current != stripRoot; current = current.parent)
			{
				segments.Add(current.name);
			}

			segments.Reverse();
			return string.Join("/", segments);
		}
	}
}
