using Application.Services;
using Application.Services.Auth;
using Application.Services.Sourdough;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class Extensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Generelt
        services.AddSingleton(TimeProvider.System);
        
        // Auth
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IDeviceRegistryService, DeviceRegistryService>();
        services.AddScoped<IMultiDeviceIdentityService, MultiDeviceIdentityService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IIdentityVerificationService, IdentityVerificationService>();
        services.AddScoped<IPasswordlessAuthService, PasswordlessAuthService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IUserRoleService, UserRoleService>();
        
        // Sourdough
        services.AddScoped<ISourdoughTelemetryService, SourdoughTelemetryService>();
        services.AddScoped<ISourdoughAnalyzerService, SourdoughAnalyzerService>();
        
        // Andet
        services.AddScoped<IFeatureToggleService, FeatureToggleService>();
        
        return services;
    }
}