public static class Version
{
    public const string UPLOADER = "1.1 ptr";
    public const string GAME = "2.21.0";
    
    public static string GetFullVersion() => $"v{UPLOADER} / v{GAME} or better";
}