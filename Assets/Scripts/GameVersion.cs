public static class GameVersion
{
	private const string Uploader = "2.1";
	private const string FormatVersion = "2026.3";
	private const string DefaultFormatVersion = "2026.3";

	public static string GetFullVersion() => $"v{Uploader}";
	public static string GetFullVersionFormat() => $"v{FormatVersion}";

	public static string GetDefaultFullVersionFormat() => $"v{DefaultFormatVersion}";
}