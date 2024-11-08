public static class Version
{
	private const string UPLOADER = "1.1 ptr";
	private const string GAME = "2.21.0";

	public static string GetFullVersion() => $"v{UPLOADER} / v{GAME} or better";
}