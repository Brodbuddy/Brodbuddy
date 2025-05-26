using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface IFirmwareStorageService
{
    Task<byte[]> ReadFirmwareAsync(Guid firmwareId);
    Task SaveFirmwareAsync(Guid firmwareId, byte[] data);
    bool FirmwareExists(Guid firmwareId);
    uint CalculateCrc32(byte[] data);
}

public class FirmwareStorageService : IFirmwareStorageService
{
    private readonly ILogger<FirmwareStorageService> _logger;
    private readonly string _firmwarePath;

    public FirmwareStorageService(ILogger<FirmwareStorageService> logger)
    {
        _logger = logger;
        _firmwarePath = Path.Combine(Directory.GetCurrentDirectory(), "firmware");
        Directory.CreateDirectory(_firmwarePath);
    }

    public async Task<byte[]> ReadFirmwareAsync(Guid firmwareId)
    {
        var filePath = GetFirmwarePath(firmwareId);
        if (!File.Exists(filePath)) throw new FileNotFoundException($"Firmware file not found: {filePath}");
        
        return await File.ReadAllBytesAsync(filePath);
    }

    public async Task SaveFirmwareAsync(Guid firmwareId, byte[] data)
    {
        var filePath = GetFirmwarePath(firmwareId);
        await File.WriteAllBytesAsync(filePath, data);
        _logger.LogInformation("Saved firmware {FirmwareId}, size: {Size} bytes", firmwareId, data.Length);
    }

    public bool FirmwareExists(Guid firmwareId)
    {
        return File.Exists(GetFirmwarePath(firmwareId));
    }

    public uint CalculateCrc32(byte[] data)
    {
        const uint polynomial = 0xEDB88320;
        uint crc = 0xFFFFFFFF;
        
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc >> 1) ^ ((crc & 1) * polynomial);
            }
        }
        
        return ~crc;
    }

    private string GetFirmwarePath(Guid firmwareId)
    {
        return Path.Combine(_firmwarePath, $"firmware_{firmwareId}.bin");
    }
}