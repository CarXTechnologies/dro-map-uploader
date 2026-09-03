using System;
using System.Linq;

namespace MapUploader.Rules
{
	public static class MapMetaRules
	{
		private const string AllowedTitlePunctuation = " -_'.,:!?()&+/";

		public static bool IsValidMapName(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			if (value != value.Trim())
			{
				return false;
			}

			return value.All(character =>
				char.IsLetterOrDigit(character) || AllowedTitlePunctuation.IndexOf(character) >= 0);
		}

		public static string DescribeMapNameRule()
		{
			return "Use letters, digits, spaces and " + AllowedTitlePunctuation.Trim() +
			       ", with no leading or trailing spaces.";
		}

		public static bool IsValidSceneName(string value)
		{
			if (string.IsNullOrEmpty(value) || !char.IsLetter(value[0]))
			{
				return false;
			}

			return value.All(character => char.IsLetterOrDigit(character) || character == '_');
		}

		public static string DescribeSceneNameRule()
		{
			return "Start with a letter and use only letters, digits and underscores.";
		}

		public static string BuildSummary(string mapName, string description, string summary)
		{
			if (!string.IsNullOrWhiteSpace(summary))
			{
				return FirstLine(summary);
			}

			if (!string.IsNullOrWhiteSpace(description))
			{
				return FirstLine(description);
			}

			return string.IsNullOrWhiteSpace(mapName) ? string.Empty : $"{mapName.Trim()} track.";
		}

		public static string ResolveVersion(string authored, string uploaderVersion, bool supportsVersion)
		{
			if (!supportsVersion)
			{
				return string.Empty;
			}

			return string.IsNullOrWhiteSpace(authored) ? uploaderVersion : authored.Trim();
		}

		public static string GetSceneNameFromPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return string.Empty;
			}

			var name = path;
			var separator = path.LastIndexOfAny(new[] { '/', '\\' });

			if (separator >= 0)
			{
				name = path.Substring(separator + 1);
			}

			return name.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
				? name.Substring(0, name.Length - ".unity".Length)
				: name;
		}

		private static string FirstLine(string value)
		{
			var trimmed = value.Trim();
			var lineBreak = trimmed.IndexOfAny(new[] { '\r', '\n' });

			return (lineBreak == -1 ? trimmed : trimmed.Substring(0, lineBreak)).Trim();
		}
	}
}
