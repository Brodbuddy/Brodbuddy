using Application.Models;

namespace Application.Interfaces.Data.Repositories;

public interface ITelemetryRepository
{
    Task<Guid> SaveReadingAsync(TelemetryReading telemetryReading);
    Task<IEnumerable<TelemetryReading>> GetAllReadingsAsync(int limit = 100);
    Task<IEnumerable<TelemetryReading>> GetReadingsByDeviceAsync(string deviceId, int limit = 100);
}