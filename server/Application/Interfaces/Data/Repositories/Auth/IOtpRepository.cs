namespace Application.Interfaces.Data.Repositories.Auth;

public interface IOtpRepository
{
    Task<Guid> SaveAsync(int code);
    Task<bool> IsValidAsync(Guid id, int code);
    Task<bool> MarkAsUsedAsync(Guid id);
}