using Api.Http.Extensions;
using Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Api.Http.Middleware;

public class FeatureToggleMiddleware
{
    private readonly RequestDelegate _next;
    
    public FeatureToggleMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IFeatureToggleService toggleService)
    {
        var endpoint = context.GetEndpoint();
        var controller = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerName;
        var action = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>()?.ActionName;
        
        if (controller != null && action != null)
        {
            var featureName = $"Api.{controller}.{action}";
            
            if (!await IsFeatureEnabledForContext(featureName, context, toggleService))
            {
                context.Response.StatusCode = 404;
                return;
            }
        }

        await _next(context);
    }
    
    private static async Task<bool> IsFeatureEnabledForContext(string featureName, HttpContext context, IFeatureToggleService toggleService)
    {
        if (context.User.Identity?.IsAuthenticated != true) return await toggleService.IsEnabledAsync(featureName);
        
        try
        {
            var userId = context.User.GetUserId();
            return await toggleService.IsEnabledForUserAsync(featureName, userId);
        }
        catch (ArgumentException)
        {
            return await toggleService.IsEnabledAsync(featureName);
        }
    }
}