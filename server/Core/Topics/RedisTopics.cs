namespace Core.Topics;

public static class RedisTopics
{
    public static string Telemetry(Guid id) => $"telemetry:{id}";
}