using Application.Services;
using Microsoft.Extensions.DependencyInjection;
namespace Application;

public static class Extensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System); 
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IOtpService, OtpService>(); 
        return services;
    }
}