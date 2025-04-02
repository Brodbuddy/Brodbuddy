using Application.Interfaces;

namespace Application.Services;

public interface IOtp
{
    Task<int> GenerateAsync();
}

public class OtpService : IOtp
{
    private readonly IOtpRepository _otpRepository;
    private readonly Random _random;

    public OtpService(IOtpRepository otpRepository)
    {
        _otpRepository = otpRepository;
        _random = new Random();
    }

    public async Task<int> GenerateAsync()
    {
        int code = _random.Next(100000, 999999);

        await _otpRepository.GenerateAsync(code);

        return code;
    }

}