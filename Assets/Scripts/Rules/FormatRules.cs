using System;

namespace MapUploader.Rules
{
	/// <summary>
	/// Which mod format may be built by which editor.
	/// </summary>
	/// <remarks>
	/// dro1 packs the scene into Unity AssetBundles, and an asset bundle is only readable by the exact engine version
	/// that wrote it. The shipped game still runs 2023.2.20f1, so a dro1 bundle built by any other editor loads as
	/// nothing at all - the map publishes fine and fails only on the player's machine. That failure mode is why the
	/// mismatch is a hard block rather than a warning. dro2 exports plain data and does not care.
	/// </remarks>
	public static class FormatRules
	{
		public static bool IsEditorVersionCompatible(string requiredVersion, string editorVersion)
		{
			if (string.IsNullOrWhiteSpace(requiredVersion))
			{
				return true;
			}

			return string.Equals(requiredVersion.Trim(), editorVersion?.Trim(), StringComparison.Ordinal);
		}

		public static string DescribeDro1Block(string requiredVersion, string editorVersion)
		{
			return $"dro1 builds are locked to Unity {requiredVersion}: the game loads dro1 asset bundles with that " +
			       $"exact engine version, and this editor is {editorVersion}. A bundle built here would publish " +
			       $"fine and load as an empty map in game. Build as dro2, or open the project in {requiredVersion}.";
		}
	}
}
