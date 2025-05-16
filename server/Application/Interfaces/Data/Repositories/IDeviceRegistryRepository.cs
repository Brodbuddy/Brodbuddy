namespace Application.Interfaces.Data.Repositories;

public interface IDeviceRegistryRepository
{
    Task<Guid> SaveAsync(Guid userId, Guid deviceId, string fingerprint);
    Task<Guid?> GetDeviceIdByFingerprintAsync(Guid userId, string fingerprint);
    Task<int> CountByUserIdAsync(Guid userId);
}