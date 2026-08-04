public static class GameVersion
{
	private const string Uploader = "v3.0";
	private const string FormatVersion = "1.0";
	private const string DefaultFormatVersion = "1.0";

	public static string GetFullVersion() => $"v{Uploader}";
	public static string GetFullVersionFormat() => $"v{FormatVersion}";

	public static string GetDefaultFullVersionFormat() => $"v{DefaultFormatVersion}";
}