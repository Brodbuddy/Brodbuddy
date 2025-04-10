using Application;
using Application.Interfaces;
using Infrastructure.Data.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Data;

public static class Extensions
{
    public static IServiceCollection AddDataInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<PostgresDbContext>((service, options) =>
        {
            var provider = services.BuildServiceProvider();
            options.UseNpgsql(provider.GetRequiredService<IOptionsMonitor<AppOptions>>().CurrentValue.Postgres.ConnectionString);
            options.EnableSensitiveDataLogging();
        });

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, PostgresOtpRepository>();
        services.AddScoped<IUserIdentityRepository, PostgresUserIdentityRepository>();
        services.AddScoped<IDeviceRepository, PostgresDeviceRepository>();
        services.AddScoped<IDeviceRegistryRepository, PostgresDeviceRegistryRepository>();
        return services;
    }
}

