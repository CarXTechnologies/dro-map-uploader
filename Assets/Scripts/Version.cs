public static class Version
{
	private const string UPLOADER = "2.1";
	private const string GAME = "2.21.0";

	public static string GetFullVersion() => $"v{UPLOADER} / v{GAME} or better";
}