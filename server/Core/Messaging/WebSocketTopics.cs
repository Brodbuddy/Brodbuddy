namespace Core.Messaging;

public static class WebSocketTopics
{
    public static class User
    {
        public static string SourdoughData(Guid userId) => $"sourdough-data:{userId}";
        public static string OtaProgress(Guid analyzerId) => $"ota-progress:{analyzerId}";
    }
    
    public static class Everyone
    {
        public static string FirmwareAvailable => "firmware-available";
    }

    public static class Admin
    {
        public static string AllDiagnostics => "admin:diagnostics";
        public static string DiagnosticsResponse(Guid analyzerId) => $"admin:diagnostics:{analyzerId}";
    }
}