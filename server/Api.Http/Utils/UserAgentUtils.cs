using Microsoft.AspNetCore.Http;

namespace Api.Http.Utils;

public static class UserAgentUtils
{
    public static string GetBrowser(HttpContext? context)
    {
        var userAgent = GetUserAgent(context);
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        if (userAgent.Contains("Chrome")) return "Chrome";
        if (userAgent.Contains("Firefox")) return "Firefox"; 
        if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Safari";
        if (userAgent.Contains("Edge")) return "Edge";
        if (userAgent.Contains("Opera")) return "Opera";
        return "Unknown";
    }

    public static string GetOperatingSystem(HttpContext? context)
    {
        var userAgent = GetUserAgent(context);
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        if (userAgent.Contains("Windows")) return "Windows";
        if (userAgent.Contains("Mac")) return "MacOS";
        if (userAgent.Contains("Linux")) return "Linux";
        if (userAgent.Contains("Android")) return "Android";
        if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) return "iOS";
        return "Unknown";
    }

    private static string? GetUserAgent(HttpContext? context)
    {
        return context?.Request.Headers["User-Agent"].ToString();
    }
}