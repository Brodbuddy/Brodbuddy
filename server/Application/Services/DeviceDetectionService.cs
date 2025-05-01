using Microsoft.AspNetCore.Http;

namespace Application.Services;

public interface IDeviceDetectionService
{
    string GetBrowser(HttpContext? context = null);
    string GetOperatingSystem(HttpContext? context = null);
}

public class DeviceDetectionService : IDeviceDetectionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeviceDetectionService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetBrowser(HttpContext? context = null)
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

    public string GetOperatingSystem(HttpContext? context = null)
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

    private string? GetUserAgent(HttpContext? context = null)
    {
        var ctx = context ?? _httpContextAccessor.HttpContext;
        
        return ctx?.Request.Headers["User-Agent"].ToString();
    }
}