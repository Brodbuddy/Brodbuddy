using Core.Entities;

namespace Application.Interfaces.Data.Repositories;

public interface IFirmwareRepository
{
    Task<FirmwareVersion?> GetByIdAsync(Guid id);
    Task<IEnumerable<FirmwareVersion>> GetAllAsync();
    Task<FirmwareVersion> CreateAsync(FirmwareVersion firmware);
    Task<FirmwareVersion?> GetLatestStableAsync();
    
    Task<FirmwareUpdate> CreateUpdateAsync(FirmwareUpdate update);
    Task UpdateUpdateStatusAsync(Guid updateId, string status, int? progress = null);
    Task<FirmwareUpdate?> GetActiveUpdateForAnalyzerAsync(Guid analyzerId);
}