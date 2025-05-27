using System.Globalization;
using System.Reflection;
using Application.Interfaces;
using Application.Interfaces.Data;
using Application.Services.Auth;
using Core.Entities;
using Infrastructure.Data.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Startup.Services;

public class SeederService : ISeederService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SeederService> _logger;
    private readonly IHostEnvironment _environment;

    private readonly HashSet<string> _excludedHttpEndpoints = new()
    {
        "TestToken",
        "RefreshToken",
        "InitiateLogin",
        "CompleteLogin",
        "GetUserInfo",
        "Logout"
    };

    private readonly HashSet<string> _excludedHttpControllers = new()
    {
        "Feature",
        "Logging",
        "PasswordlessAuth"
    };

    private readonly HashSet<string> _excludedWebSocketHandlers = new()
    {
        "PingHandler"
    };

    public SeederService(IServiceProvider serviceProvider, ILogger<SeederService> logger, IHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _environment = environment;
    }

    public async Task SeedFeaturesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        await transactionManager.ExecuteInTransactionAsync(async () =>
        {
            var features = DiscoverEndpointFeatures();
            await CreateMissingFeaturesAsync(features, scope.ServiceProvider);
        });
    }

    private List<EndpointFeature> DiscoverEndpointFeatures()
    {
        var features = new List<EndpointFeature>();

        features.AddRange(DiscoverHttpEndpoints());
        features.AddRange(DiscoverWebSocketHandlers());

        return features;
    }

    private List<EndpointFeature> DiscoverHttpEndpoints()
    {
        var features = new List<EndpointFeature>();

        var httpControllerTypes = Assembly.GetAssembly(typeof(Api.Http.Controllers.AnalyzerController))!
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract);

        foreach (var controllerType in httpControllerTypes)
        {
            var controllerName = controllerType.Name.Replace("Controller", "", StringComparison.Ordinal);
            
            if (_excludedHttpControllers.Contains(controllerName))
                continue;
                
            var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == controllerType);

            foreach (var method in methods)
            {
                var httpMethods = GetHttpMethods(method);
                if (httpMethods.Count <= 0 || _excludedHttpEndpoints.Contains(method.Name)) continue;
                var routeTemplate = GetRouteTemplate(controllerType, method);
                features.Add(new EndpointFeature
                {
                    Name = $"Api.{controllerName}.{method.Name}",
                    Description = $"{string.Join("/", httpMethods)} {routeTemplate}",
                    Category = "HTTP"
                });
            }
        }

        return features;
    }

    private List<EndpointFeature> DiscoverWebSocketHandlers()
    {
        var features = new List<EndpointFeature>();

        var websocketHandlerTypes = Assembly.GetAssembly(typeof(Api.Websocket.EventHandlers.PingHandler))!
            .GetTypes()
            .Where(t => !t.IsAbstract && 
                       t.GetInterfaces().Any(i => i.IsGenericType && 
                                                 i.GetGenericTypeDefinition() == typeof(Brodbuddy.WebSocket.Core.IWebSocketHandler<,>)));

        features.AddRange(websocketHandlerTypes
            .Where(handlerType => !_excludedWebSocketHandlers.Contains(handlerType.Name))
            .Select(handlerType => new EndpointFeature
            {
                Name = $"Websocket.{handlerType.Name.Replace("Handler", "", StringComparison.Ordinal)}",
                Description = $"WebSocket event handler for {handlerType.Name.Replace("Handler", "", StringComparison.Ordinal)} messages",
                Category = "WebSocket"
            }));

        return features;
    }

    private static List<string> GetHttpMethods(MethodInfo method)
    {
        var httpMethods = new List<string>();
        
        if (method.GetCustomAttribute<HttpGetAttribute>() != null) httpMethods.Add("GET");
        if (method.GetCustomAttribute<HttpPostAttribute>() != null) httpMethods.Add("POST");
        if (method.GetCustomAttribute<HttpPutAttribute>() != null) httpMethods.Add("PUT");
        if (method.GetCustomAttribute<HttpDeleteAttribute>() != null) httpMethods.Add("DELETE");
        if (method.GetCustomAttribute<HttpPatchAttribute>() != null) httpMethods.Add("PATCH");

        return httpMethods;
    }

    private static string GetRouteTemplate(Type controllerType, MethodInfo method)
    {
        var controllerRoute = controllerType.GetCustomAttribute<RouteAttribute>()?.Template ?? "";
        var actionRoute = method.GetCustomAttribute<RouteAttribute>()?.Template ??
                         method.GetCustomAttribute<HttpGetAttribute>()?.Template ??
                         method.GetCustomAttribute<HttpPostAttribute>()?.Template ??
                         method.GetCustomAttribute<HttpPutAttribute>()?.Template ??
                         method.GetCustomAttribute<HttpDeleteAttribute>()?.Template ??
                         method.Name.ToLower(CultureInfo.InvariantCulture);

        return $"{controllerRoute}/{actionRoute}".Replace("[controller]", controllerType.Name.Replace("Controller", "", StringComparison.Ordinal).ToLower(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private async Task CreateMissingFeaturesAsync(List<EndpointFeature> discoveredFeatures, IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<PgDbContext>();
        var existingFeatures = await dbContext.Features.Select(f => f.Name).ToHashSetAsync();

        var newFeatures = discoveredFeatures
            .Where(f => !existingFeatures.Contains(f.Name))
            .Select(f => new Feature
            {
                Id = Guid.NewGuid(),
                Name = f.Name,
                Description = f.Description,
                IsEnabled = true,
                RolloutPercentage = 100,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        if (newFeatures.Count > 0)
        {
            dbContext.Features.AddRange(newFeatures);
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} new feature toggles: {Features}", 
                newFeatures.Count, 
                string.Join(", ", newFeatures.Select(f => f.Name)));
        }
        else
        {
            _logger.LogInformation("No new features to seed");
        }
    }

    public async Task SeedAdminAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();

        await transactionManager.ExecuteInTransactionAsync(async () =>
        {
            var userIdentityService = scope.ServiceProvider.GetRequiredService<IUserIdentityService>();
            var userRoleService = scope.ServiceProvider.GetRequiredService<IUserRoleService>();

            const string adminEmail = "brodbuddy@proton.me";
            
            if (await userIdentityService.ExistsAsync(adminEmail))
            {
                _logger.LogInformation("Admin user {Email} already exists, skipping admin seeding", adminEmail);
                return;
            }

            var adminUserId = await userIdentityService.CreateAsync(adminEmail);
            await userRoleService.AssignRoleAsync(adminUserId, Role.Admin);
            
            _logger.LogInformation("Created admin user {Email} with admin role", adminEmail);
        });
    }

    public async Task SeedTestDataAsync(bool clear = false)
    {
        if (!_environment.IsDevelopment())
        {
            _logger.LogInformation("Skipping test data seeding - not in development environment");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PgDbContext>();

        await transactionManager.ExecuteInTransactionAsync(async () =>
        {
            if (clear)
            {
                _logger.LogInformation("Clearing existing test data");
                await ClearTestDataAsync(dbContext);
            }

            var hasTestData = await dbContext.Users.AnyAsync(u => u.Email.StartsWith("test"));
            if (hasTestData && !clear)
            {
                _logger.LogInformation("Test data already exists, skipping test data seeding (use clear=true to reset)");
                return;
            }

            await SeedUsers(scope.ServiceProvider, 10);
            await SeedAnalyzers(scope.ServiceProvider, 40);
        });
    }

    private async Task SeedUsers(IServiceProvider serviceProvider, int count)
    {
        var userIdentityService = serviceProvider.GetRequiredService<IUserIdentityService>();
        var userRoleService = serviceProvider.GetRequiredService<IUserRoleService>();

        for (int i = 1; i <= count; i++)
        {
            var email = $"test.user{i:D2}@brodbuddy.com";
            var userId = await userIdentityService.CreateAsync(email);
            
            if (i <= 2)
            {
                await userRoleService.AssignRoleAsync(userId, Role.Admin);
            }
        }

        _logger.LogInformation("Created {Count} test users", count);
    }

    private async Task SeedAnalyzers(IServiceProvider serviceProvider, int count)
    {
        var dbContext = serviceProvider.GetRequiredService<PgDbContext>();
        var random = new Random();
        var analyzers = new List<SourdoughAnalyzer>();

        for (int i = 1; i <= count; i++)
        {
            var macBytes = new byte[6];
            random.NextBytes(macBytes);
            macBytes[0] = 0xAA;
            var macAddress = string.Join(":", macBytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));

            var isActivated = random.Next(100) < 50;
            var createdAt = DateTime.UtcNow.AddDays(-random.Next(1, 30));

            analyzers.Add(new SourdoughAnalyzer 
            {
                Id = Guid.NewGuid(),
                MacAddress = macAddress,
                Name = $"Test Analyzer {i:D2}",
                ActivationCode = $"TEST{i:D8}",
                IsActivated = isActivated,
                ActivatedAt = isActivated ? createdAt.AddDays(random.Next(1, 7)) : null,
                CreatedAt = createdAt,
                LastSeen = isActivated ? DateTime.UtcNow.AddMinutes(-random.Next(10, 1440)) : null,
                FirmwareVersion = $"1.{random.Next(0, 5)}.{random.Next(0, 10)}-test"
            });
        }

        dbContext.SourdoughAnalyzers.AddRange(analyzers);
        await dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Created {Count} test analyzers", count);
    }

    private async Task ClearTestDataAsync(PgDbContext dbContext)
    {
        var testUsers = dbContext.Users.Where(u => u.Email.StartsWith("test"));
        var testAnalyzers = dbContext.SourdoughAnalyzers.Where(a => a.Name != null && a.Name.StartsWith("Test"));
        
        dbContext.Users.RemoveRange(testUsers);
        dbContext.SourdoughAnalyzers.RemoveRange(testAnalyzers);
        
        await dbContext.SaveChangesAsync();
        _logger.LogInformation("Cleared existing test data");
    }

    private sealed class EndpointFeature
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Category { get; set; } = null!;
    }
}