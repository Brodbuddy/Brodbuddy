using Application.Interfaces.Communication.Notifiers;
using Application.Interfaces.Data.Repositories;
using Core.Entities;
using Core.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface IFirmwareUpdateBackgroundService
{
    Task StartFirmwareTransferAsync(Guid analyzerId, Guid updateId, byte[] firmwareData);
}

public class FirmwareUpdateBackgroundService : IFirmwareUpdateBackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FirmwareUpdateBackgroundService> _logger;

    public FirmwareUpdateBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FirmwareUpdateBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartFirmwareTransferAsync(Guid analyzerId, Guid updateId, byte[] firmwareData)
    {
        _ = Task.Run(async () => await ExecuteTransferAsync(analyzerId, updateId, firmwareData));
        return Task.CompletedTask;
    }

    private async Task ExecuteTransferAsync(Guid analyzerId, Guid updateId, byte[] firmwareData)
    {
        using var scope = _serviceProvider.CreateScope();
        var firmwareRepo = scope.ServiceProvider.GetRequiredService<IFirmwareRepository>();
        var userNotifier = scope.ServiceProvider.GetRequiredService<IUserNotifier>();
        var transferService = scope.ServiceProvider.GetRequiredService<IFirmwareTransferService>();

        try
        {
            _logger.LogInformation("Starting firmware transfer for update {UpdateId}", updateId);
            
            await firmwareRepo.UpdateUpdateStatusAsync(updateId, FirmwareUpdate.OtaStatus.Downloading, 0);
            await userNotifier.NotifyOtaProgressAsync(analyzerId, new OtaProgressUpdate(
                analyzerId,
                updateId,
                FirmwareUpdate.OtaStatus.Downloading,
                0,
                "Starting firmware download"
            ));

            await transferService.SendFirmwareAsync(analyzerId, firmwareData);
            
            _logger.LogInformation("Firmware transfer completed for update {UpdateId}", updateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transfer firmware for update {UpdateId}", updateId);
            
            await firmwareRepo.UpdateUpdateStatusAsync(updateId, FirmwareUpdate.OtaStatus.Failed);
            await userNotifier.NotifyOtaProgressAsync(analyzerId, new OtaProgressUpdate(
                analyzerId,
                updateId,
                FirmwareUpdate.OtaStatus.Failed,
                0,
                $"Firmware transfer failed: {ex.Message}"
            ));
        }
    }
}