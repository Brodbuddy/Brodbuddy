namespace Core.Messaging;

public static class WebSocketTopics
{
    public static class User
    {
        public static string SourdoughData(Guid userId) => $"sourdough-data:{userId}";
    }

    public static class Admin
    {
        public static string AllDiagnostics => "admin:diagnostics";
        public static string DiagnosticsResponse(Guid analyzerId) => $"admin:diagnostics:{analyzerId}";
    }
}