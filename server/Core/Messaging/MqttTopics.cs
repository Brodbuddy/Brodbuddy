namespace Core.Messaging;

public static class MqttTopics
{
    public static class Analyzer 
    {
        public static string Telemetry(Guid deviceId) => $"analyzers/{deviceId}/telemetry";
    }
    
    public static class Patterns
    {
        public const string AllAnalyzersTelemetry = "analyzer/+/telemetry";
    }

    public static Guid ExtractAnalyzerId(string? topic, string template = "analyzer/{analyzerId}/telemetry")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var topicParts = topic.Split('/');
        var templateParts = template.Split('/');

        if (topicParts.Length != templateParts.Length) throw new ArgumentException($"Topic '{topic}' doesn't match template '{template}'");

        for (int i = 0; i < templateParts.Length; i++)
        {
            if (templateParts[i] != "{analyzerId}") continue;
            if (Guid.TryParse(topicParts[i], out var analyzerId)) return analyzerId;
            throw new ArgumentException($"Invalid analyzer ID format: {topicParts[i]}");
        }

        throw new ArgumentException($"Template '{template}' doesn't contain {{analyzerId}} placeholder");
    }
}