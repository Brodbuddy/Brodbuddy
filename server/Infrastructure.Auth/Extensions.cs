using Application.Interfaces.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, JwtAuthenticationService>();
        return services;
    }
}