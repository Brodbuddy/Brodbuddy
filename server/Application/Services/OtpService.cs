using System.Security.Cryptography;
using Application.Interfaces.Data.Repositories;

namespace Application.Services;

public interface IOtpService
{
    Task<(Guid id, int code)> GenerateAsync();
    Task<bool> IsValidAsync(Guid id, int code);
    Task<bool> MarkAsUsedAsync(Guid id);
}

public class OtpService : IOtpService
{
    private readonly IOtpRepository _otpRepository;
    
    public OtpService(IOtpRepository otpRepository)
    {
        _otpRepository = otpRepository;
    }
    
    public async Task<(Guid id, int code)> GenerateAsync()
    {
        //Benytter RandomNumberGenerator da det er kryptografisk sikker
        int code = RandomNumberGenerator.GetInt32(100000, 999999);

        Guid id = await _otpRepository.SaveAsync(code);

        return (id, code);
    }

    public async Task<bool> IsValidAsync(Guid id, int code)
    {
        if (id == Guid.Empty) throw new ArgumentException("OTP ID cannot be empty", nameof(id));
        
        if (code is < 100000 or > 999999)
        {
            return false;
        }

        return await _otpRepository.IsValidAsync(id, code);
    }

    public async Task<bool> MarkAsUsedAsync(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("OTP ID cannot be empty", nameof(id));
        
        return await _otpRepository.MarkAsUsedAsync(id);
    }
}