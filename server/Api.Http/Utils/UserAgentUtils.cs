using UAParser;

namespace Api.Http.Utils;

public static class UserAgentUtils
{
    private static readonly Parser Parser = Parser.GetDefault();
    private const string Unknown = "Unknown";
    
    public static string GetBrowser(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return Unknown;

        var client = Parser.Parse(userAgent);
        return string.IsNullOrEmpty(client.UA.Family) 
            ? Unknown 
            : client.UA.Family;
    }

    public static string GetOperatingSystem(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return Unknown;

        var client = Parser.Parse(userAgent);
        return string.IsNullOrEmpty(client.OS.Family) 
            ? Unknown 
            : client.OS.Family;
    }
}