using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class Extensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IDeviceRegistryService, DeviceRegistryService>();
        services.AddScoped<IMultiDeviceIdentityService, MultiDeviceIdentityService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IIdentityVerificationService, IdentityVerificationService>();
        services.AddScoped<IPasswordlessAuthService, PasswordlessAuthService>();

        services.AddScoped<IMqttTestService, MqttTestService>();
        return services;
    }
}