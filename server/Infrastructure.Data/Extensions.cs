using Application;
using Application.Interfaces;
using Application.Interfaces.Data.Repositories;
using Infrastructure.Data.Persistence;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Data;

public static class Extensions
{
    public static IServiceCollection AddDataInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<PgDbContext>((_, options) =>
        {
            var provider = services.BuildServiceProvider();
            options.UseNpgsql(provider.GetRequiredService<IOptionsMonitor<AppOptions>>().CurrentValue.Postgres.ConnectionString);
            options.EnableSensitiveDataLogging();
        });
        
        var serviceProvider = services.BuildServiceProvider();
        var appOptions = serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value;
        services.AddStackExchangeRedisCache(options => {
            options.Configuration = appOptions.Dragonfly.ConnectionString;
        });

        services.AddScoped<IRefreshTokenRepository, PgRefreshTokenRepository>();
        services.AddScoped<IOtpRepository, PgOtpRepository>();
        services.AddScoped<IUserIdentityRepository, PgUserIdentityRepository>();
        services.AddScoped<IDeviceRepository, PgDeviceRepository>();
        services.AddScoped<IDeviceRegistryRepository, PgDeviceRegistryRepository>();
        services.AddScoped<IMultiDeviceIdentityRepository, PgMultiDeviceIdentityRepository>();
        services.AddScoped<IIdentityVerificationRepository, PgIdentityVerificationRepository>();
        services.AddScoped<IRoleRepository, PgRoleRepository>();
        services.AddScoped<IUserRoleRepository, PgUserRoleRepository>();
        
        services.AddScoped<IFeatureToggleRepository, PgFeatureToggleRepository>();
        return services;
    }
}