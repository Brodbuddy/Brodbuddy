using Application.Interfaces.Communication.Notifiers;
using Application.Interfaces.Communication.Publishers;
using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Data.Repositories.Sourdough;
using Application.Models.DTOs;
using Core.Entities;
using Core.Exceptions;
using Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface IOtaService
{
    Task<Guid> StartOtaUpdateAsync(Guid analyzerId, Guid firmwareVersionId);
    Task ProcessOtaStatusAsync(Guid analyzerId, OtaStatusMessage status);
}

public class OtaService : IOtaService
{
    private readonly IFirmwareRepository _firmwareRepository;
    private readonly ISourdoughAnalyzerRepository _analyzerRepository;
    private readonly IFirmwareStorageService _firmwareStorage;
    private readonly IFirmwareUpdateBackgroundService _backgroundService;
    private readonly IOtaPublisher _otaPublisher;
    private readonly IUserNotifier _userNotifier;
    private readonly ILogger<OtaService> _logger;

    public OtaService(
        IFirmwareRepository firmwareRepository,
        ISourdoughAnalyzerRepository analyzerRepository,
        IFirmwareStorageService firmwareStorage,
        IFirmwareUpdateBackgroundService backgroundService,
        IOtaPublisher otaPublisher,
        IUserNotifier userNotifier,
        ILogger<OtaService> logger)
    {
        _firmwareRepository = firmwareRepository;
        _analyzerRepository = analyzerRepository;
        _firmwareStorage = firmwareStorage;
        _backgroundService = backgroundService;
        _otaPublisher = otaPublisher;
        _userNotifier = userNotifier;
        _logger = logger;
    }

    public async Task<Guid> StartOtaUpdateAsync(Guid analyzerId, Guid firmwareVersionId)
    {
        if (!_firmwareStorage.FirmwareExists(firmwareVersionId)) throw new EntityNotFoundException($"Firmware not found: {firmwareVersionId}");

        var firmwareData = await _firmwareStorage.ReadFirmwareAsync(firmwareVersionId);
        var crc32 = _firmwareStorage.CalculateCrc32(firmwareData);
        
        var firmware = await _firmwareRepository.GetByIdAsync(firmwareVersionId);
        if (firmware == null) throw new EntityNotFoundException($"Firmware version not found: {firmwareVersionId}");
        
        var version = firmware.Version;

        var update = new FirmwareUpdate
        {
            AnalyzerId = analyzerId,
            FirmwareVersionId = firmwareVersionId,
            Status = FirmwareUpdate.OtaStatus.Started,
            Progress = 0
        };
        await _firmwareRepository.CreateUpdateAsync(update);

        await _otaPublisher.PublishStartOtaAsync(new OtaStartCommand(
            analyzerId.ToString(),
            version,
            firmwareData.Length,
            crc32 
        ));

        await _userNotifier.NotifyOtaProgressAsync(analyzerId, new OtaProgressUpdate(
            analyzerId,
            update.Id,
            FirmwareUpdate.OtaStatus.Started,
            0,
            $"OTA update started for firmware version {version} ({firmwareData.Length} bytes)"
        ));

        _logger.LogInformation("Started OTA update {UpdateId} for analyzer {AnalyzerId}", update.Id, analyzerId);

        await _backgroundService.StartFirmwareTransferAsync(analyzerId, update.Id, firmwareData);
        
        return update.Id;
    }
    
    
    public async Task ProcessOtaStatusAsync(Guid analyzerId, OtaStatusMessage status)
    {
        var activeUpdate = await _firmwareRepository.GetActiveUpdateForAnalyzerAsync(analyzerId);
        if (activeUpdate == null)
        {
            _logger.LogWarning("No active OTA update found for analyzer {AnalyzerId}", analyzerId);
            return;
        }
        
        if (status.Status != activeUpdate.Status || status.Progress != activeUpdate.Progress)
        {
            await _firmwareRepository.UpdateUpdateStatusAsync(activeUpdate.Id, status.Status, status.Progress);
            
            _logger.LogInformation("Updated OTA status for update {UpdateId}: {Status} at {Progress}%", 
                activeUpdate.Id, status.Status, status.Progress);
        }

        if (status.Status == FirmwareUpdate.OtaStatus.Complete)
        {
            var firmware = await _firmwareRepository.GetByIdAsync(activeUpdate.FirmwareVersionId);
            if (firmware != null)
            {
                var analyzer = await _analyzerRepository.GetByIdAsync(analyzerId);
                if (analyzer != null)
                {
                    analyzer.FirmwareVersion = firmware.Version;
                    await _analyzerRepository.UpdateAsync(analyzer);
                    
                    _logger.LogInformation("Updated analyzer {AnalyzerId} firmware version to {Version}", 
                        analyzerId, firmware.Version);
                }
            }
        }
        
        await _userNotifier.NotifyOtaProgressAsync(analyzerId, new OtaProgressUpdate(
            analyzerId,
            activeUpdate.Id,
            status.Status,
            status.Progress,
            status.Message ?? $"OTA {status.Status} - {status.Progress}%"
        ));
    }
}