using Application.Models;

namespace Application.Interfaces.Data.Repositories;

public interface ITelemetryRepository
{
    Task<Guid> SaveReadingAsync(string deviceId, double distance, double risePercentage, DateTime timestamp);
    Task<IEnumerable<TelemetryReading>> GetAllReadingsAsync(int limit = 100);
    Task<IEnumerable<TelemetryReading>> GetReadingsByDeviceAsync(string deviceId, int limit = 100);
}