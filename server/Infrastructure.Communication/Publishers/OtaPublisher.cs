using Application.Interfaces.Communication.Publishers;
using Application.Models.DTOs;
using HiveMQtt.MQTT5.Types;
using Infrastructure.Communication.Mqtt;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Communication.Publishers;

public class OtaPublisher : IOtaPublisher
{
    private readonly IMqttPublisher _mqttPublisher;
    private readonly ILogger<OtaPublisher> _logger;

    public OtaPublisher(IMqttPublisher mqttPublisher, ILogger<OtaPublisher> logger)
    {
        _mqttPublisher = mqttPublisher;
        _logger = logger;
    }

    public async Task PublishStartOtaAsync(OtaStartCommand command)
    {
        var topic = $"analyzer/{command.AnalyzerId}/ota/start";
        var payload = new
        {
            version = command.Version,
            size = command.Size,
            crc32 = command.Crc32
        };
        
        await _mqttPublisher.PublishAsync(topic, payload);
        
        _logger.LogInformation("Published OTA start to {Topic}: version={Version}, size={Size}, crc32={Crc32}", 
            topic, command.Version, command.Size, command.Crc32);
    }

    public async Task PublishOtaChunkAsync(OtaChunkCommand command)
    {
        var topic = $"analyzer/{command.AnalyzerId}/ota/chunk";
        
        if (command.ChunkIndex % 10 == 0 || command.ChunkIndex == 0)
        {
            _logger.LogInformation("Publishing OTA chunk {ChunkIndex} to {Topic}, size={ChunkSize}", 
                command.ChunkIndex, topic, command.ChunkSize);
        }
        
        // Byg binær payload: [4 bytes index][4 bytes size][chunk data]
        var payload = new byte[8 + command.ChunkData.Length];
        BitConverter.GetBytes(command.ChunkIndex).CopyTo(payload, 0);
        BitConverter.GetBytes(command.ChunkSize).CopyTo(payload, 4);
        command.ChunkData.CopyTo(payload, 8);
        
        await _mqttPublisher.PublishBinaryAsync(topic, payload);
    }
}