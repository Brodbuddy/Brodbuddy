using Application.Interfaces.Communication.Notifier;
using Application.Models;
using Brodbuddy.WebSocket.State;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Communication.Notifier;

public class WsDeviceNotifier : IDeviceNotifier
{
    private readonly ISocketManager _socketManager;
    private readonly ILogger<WsDeviceNotifier> _logger;

    public WsDeviceNotifier(ISocketManager socketManager, ILogger<WsDeviceNotifier> logger)
    {
        _socketManager = socketManager;
        _logger = logger;
    }

    public async Task NotifyDeviceAsync(TelemetryReading telemetryReading)
    {
        try
        {
            var topic = $"telemetry/{telemetryReading.DeviceId}";
            
            _logger.LogInformation(
                "Broadcasting telemetry reading for device {DeviceId} on topic {Topic}",
                telemetryReading.DeviceId, topic);
            
            await _socketManager.BroadcastAsync(topic, telemetryReading);
        }
        catch (Exception e)
        {
            _logger.LogError(e, 
                "Failed to notify device {DeviceId} with telemetry reading", 
                telemetryReading.DeviceId);
        }
    }
}
