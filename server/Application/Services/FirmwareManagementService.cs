using Application.Interfaces.Data.Repositories;
using Application.Models.DTOs;
using Core.Entities;
using Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface IFirmwareManagementService
{
    Task<FirmwareMetadata> UploadFirmwareAsync(FirmwareUpload upload);
    Task<IEnumerable<FirmwareVersion>> GetAllFirmwareVersionsAsync();
}

public class FirmwareManagementService : IFirmwareManagementService
{
    private readonly IFirmwareRepository _firmwareRepository;
    private readonly IFirmwareStorageService _firmwareStorage;
    private readonly ILogger<FirmwareManagementService> _logger;

    public FirmwareManagementService(
        IFirmwareRepository firmwareRepository,
        IFirmwareStorageService firmwareStorage,
        ILogger<FirmwareManagementService> logger)
    {
        _firmwareRepository = firmwareRepository;
        _firmwareStorage = firmwareStorage;
        _logger = logger;
    }

    public async Task<FirmwareMetadata> UploadFirmwareAsync(FirmwareUpload upload)
    {
        if (upload.Data.Length == 0) throw new BusinessRuleViolationException("Firmware data cannot be empty");

        var crc32 = _firmwareStorage.CalculateCrc32(upload.Data);

        var firmware = new FirmwareVersion
        {
            Version = upload.Version,
            Description = upload.Description,
            FileSize = upload.Data.Length,
            Crc32 = crc32, 
            ReleaseNotes = upload.ReleaseNotes,
            IsStable = upload.IsStable,
            CreatedBy = upload.CreatedBy
        };

        await _firmwareRepository.CreateAsync(firmware);

        await _firmwareStorage.SaveFirmwareAsync(firmware.Id, upload.Data);

        _logger.LogInformation("Uploaded firmware {Version} with ID {FirmwareId}, size: {Size} bytes", firmware.Version, firmware.Id, firmware.FileSize);

        return new FirmwareMetadata(firmware.Id, firmware.Version, firmware.FileSize, (uint)firmware.Crc32);
    }

    public async Task<IEnumerable<FirmwareVersion>> GetAllFirmwareVersionsAsync()
    {
        return await _firmwareRepository.GetAllAsync();
    }
}