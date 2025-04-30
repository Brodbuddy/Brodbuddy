using Api.Http.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Http;

public static class ApiStartup
{
    public static IServiceCollection AddHttpApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocument();
        
        // GlobalExceptionHandling
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        
        return services;
    }

    public static WebApplication ConfigureHttpApi(this WebApplication app, int port)
    {
        app.UseExceptionHandler();
        
        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapControllers();
        app.Urls.Add($"http://0.0.0.0:{port}");
        return app;
    }
}