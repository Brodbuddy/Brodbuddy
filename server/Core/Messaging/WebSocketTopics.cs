namespace Core.Messaging;

public static class WebSocketTopics
{
    public static class User
    {
        public static string SourdoughData(Guid userId) => $"sourdough-data:{userId}";
    }
}