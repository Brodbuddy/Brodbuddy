using Api.Http.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace Api.Http.Extensions;

public static class Extensions
{
    private const string ApiTitle = "Brodbuddy API";
    private const string ApiVersion = "v1";
    private const string ApiDescription = "API til Brodbuddy";
    private const string CorsPolicy = "CorsPolicy";
    
    public static IServiceCollection AddHttpApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocument(configure =>
        {
            configure.Title = ApiTitle;
            configure.Version = ApiVersion;
            configure.Description = ApiDescription;
        
            configure.AddSecurity("JWT", [], new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                Scheme = "Bearer ",
                Name = "Authorization",
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the textbox: Bearer {your JWT token}."
            });
        
            configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
            configure.DocumentProcessors.Add(new MakeAllPropertiesRequiredProcessor());
        });

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, builder =>
            {
                builder.WithOrigins("http://localhost:5173")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); 
            });
        });
        
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddScheme<JwtBearerOptions, JwtAuthenticationHandler>(JwtBearerDefaults.AuthenticationScheme,
                _ => { }
            );

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAssertion(context =>
                {
                    if (context.Resource is not HttpContext httpContext) return false;
            
                    // Tillad OPTIONS for CORS
                    if (string.Equals(httpContext.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                        return true;

                    // Tillad swagger og root
                    if (httpContext.Request.Path.StartsWithSegments("/swagger") ||
                        httpContext.Request.Path.StartsWithSegments("/swagger-ui") ||
                        httpContext.Request.Path.StartsWithSegments("/refresh") ||
                        httpContext.Request.Path.Equals("/"))
                        return true;
                    
                    // Kræv autentificering for alt andet
                    return context.User.Identity?.IsAuthenticated ?? false;
                })
                .Build();
            
            options.AddPolicy("admin", policy => policy.RequireRole("admin"));
            options.AddPolicy("user", policy => policy.RequireRole("user"));
        });
        
        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        });
        
        services.AddHttpContextAccessor();
        
        return services;
    }

    public static WebApplication ConfigureHttpApi(this WebApplication app, int port)
    {
        app.UseOpenApi();
        
        app.UseSwaggerUi(settings =>
        {
            settings.DocumentTitle = ApiTitle;
            settings.DocExpansion = "list";
        });
        
        app.UseCors(CorsPolicy);

        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapControllers();
        app.Urls.Add($"http://0.0.0.0:{port}");
        return app;
    }
}