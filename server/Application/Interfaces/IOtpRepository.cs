using Core.Entities;

namespace Application.Interfaces;

public interface IOtpRepository
{
    Task<Guid> SaveAsync(int code);
    Task<bool> IsValidAsync(Guid id, int code);
    Task<bool> MarkAsUsedAsync(Guid id);
    Task<OneTimePassword?> GetLatestOtpAsync();
}