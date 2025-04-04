using Application;
using Application.Interfaces;
using Application.Services;
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
        
        services.AddScoped<IOtpRepository, PostgresOtpRepository>();
        return services;
    }
}

