using Core.Entities;

namespace Application.Interfaces;

public interface IOtpRepository
{
    Task<Guid> GenerateAsync(int code);
}