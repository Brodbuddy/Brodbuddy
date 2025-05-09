namespace Application.Models;

public record TelemetryReading(string? DeviceId, double Distance, double RisePercentage, DateTime Timestamp);
