using System.Security.Cryptography;
using Application.Interfaces;

namespace Application.Services;

public interface IOtpService
{
    Task<int> GenerateAsync();
}


public class OtpService : IOtpService
{
    private readonly IOtpRepository _otpRepository;


    public OtpService(IOtpRepository otpRepository)
    {
        _otpRepository = otpRepository;
    }

    public async Task<int> GenerateAsync()
    {
        //Benytter RandomNumberGenerator da det er kryptografisk sikker
        int code = RandomNumberGenerator.GetInt32(100000, 999999);

        await _otpRepository.SaveAsync(code);

        return code;
    }

   
}