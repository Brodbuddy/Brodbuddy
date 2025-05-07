namespace Application.Models;

public class TelemetryReading
{
    public string? DeviceId { get; set; }
    public double Distance { get; set; }
    public double RisePercentage { get; set; }
    public DateTime Timestamp { get; set; }
}