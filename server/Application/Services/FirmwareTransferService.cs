using Application.Interfaces.Communication.Publishers;
using Application.Models.DTOs;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface IFirmwareTransferService
{
    Task SendFirmwareAsync(Guid analyzerId, byte[] firmwareData);
}

public class FirmwareTransferService : IFirmwareTransferService
{
    private readonly IOtaPublisher _otaPublisher;
    private readonly ILogger<FirmwareTransferService> _logger;
    private const int ChunkSize = 4096;

    public FirmwareTransferService(
        IOtaPublisher otaPublisher,
        ILogger<FirmwareTransferService> logger)
    {
        _otaPublisher = otaPublisher;
        _logger = logger;
    }

    public async Task SendFirmwareAsync(Guid analyzerId, byte[] firmwareData)
    {
        var totalChunks = (firmwareData.Length + ChunkSize - 1) / ChunkSize;
        _logger.LogInformation("Starting to send {TotalChunks} chunks of firmware data, total size: {TotalSize} bytes", 
            totalChunks, firmwareData.Length);
        
        for (int i = 0; i < firmwareData.Length; i += ChunkSize)
        {
            var chunkIndex = (uint)(i / ChunkSize);
            var actualChunkSize = Math.Min(ChunkSize, firmwareData.Length - i);
            var chunkData = new byte[actualChunkSize];
            Array.Copy(firmwareData, i, chunkData, 0, actualChunkSize);
            
            if (chunkIndex % 10 == 0 || chunkIndex == 0)
            {
                _logger.LogInformation("Sending chunk {ChunkIndex}/{TotalChunks}, size: {ChunkSize}", 
                    chunkIndex + 1, totalChunks, actualChunkSize);
            }
            
            await _otaPublisher.PublishOtaChunkAsync(new OtaChunkCommand(
                analyzerId.ToString(),
                chunkData,
                chunkIndex,
                (uint)actualChunkSize
            ));
            
            // Delay mellem chunks for at undgå at overvælde ESP32's MQTT buffer
            await Task.Delay(50);
        }
        
        _logger.LogInformation("Firmware sent: {TotalChunks} chunks", totalChunks);
    }
}