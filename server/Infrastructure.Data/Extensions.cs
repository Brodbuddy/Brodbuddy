using Application;
using Application.Interfaces.Data;
using Application.Interfaces.Data.Repositories;
using Application.Interfaces.Data.Repositories.Auth;
using Application.Interfaces.Data.Repositories.Sourdough;
using Infrastructure.Data.Persistence;
using Infrastructure.Data.Repositories;
using Infrastructure.Data.Repositories.Auth;
using Infrastructure.Data.Repositories.Sourdough;
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
        
        // Generelt
        services.AddScoped<ITransactionManager, EfTransactionManager>();
        
        // Auth
        services.AddScoped<IRefreshTokenRepository, PgRefreshTokenRepository>();
        services.AddScoped<IOtpRepository, PgOtpRepository>();
        services.AddScoped<IUserIdentityRepository, PgUserIdentityRepository>();
        services.AddScoped<IDeviceRepository, PgDeviceRepository>();
        services.AddScoped<IDeviceRegistryRepository, PgDeviceRegistryRepository>();
        services.AddScoped<IMultiDeviceIdentityRepository, PgMultiDeviceIdentityRepository>();
        services.AddScoped<IIdentityVerificationRepository, PgIdentityVerificationRepository>();
        services.AddScoped<IRoleRepository, PgRoleRepository>();
        services.AddScoped<IUserRoleRepository, PgUserRoleRepository>();
        
        // Surdej
        services.AddScoped<ISourdoughAnalyzerRepository, PgSourdoughAnalyzerRepository>();
        services.AddScoped<IUserAnalyzerRepository, PgUserAnalyzerRepository>();
        
        // Andet
        services.AddScoped<IFeatureToggleRepository, PgFeatureToggleRepository>();
        services.AddScoped<IFirmwareRepository, PgFirmwareRepository>();
        
        return services;
    }
}