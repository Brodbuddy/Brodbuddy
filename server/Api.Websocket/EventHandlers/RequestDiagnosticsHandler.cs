using System.Text.Json.Serialization;
using Application.Services.Sourdough;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Core.Entities;
using Fleck;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Websocket.EventHandlers;

public record DiagnosticsRequest(
    [property: JsonPropertyName("analyzerId")] Guid AnalyzerId
);

public record DiagnosticsResponse(
    [property: JsonPropertyName("message")] string Message
);


[Authorize(Roles = Role.Admin)]
public class RequestDiagnosticsHandler(IServiceProvider serviceProvider, ILogger<RequestDiagnosticsHandler> logger) : IWebSocketHandler<DiagnosticsRequest, DiagnosticsResponse>
{
    public async Task<DiagnosticsResponse> HandleAsync(DiagnosticsRequest incoming, string clientId, IWebSocketConnection socket)
    {
        try
        {
            var service = serviceProvider.GetRequiredService<ISourdoughTelemetryService>();
            await service.ProcessDiagnosticsRequestAsync(incoming.AnalyzerId);
            
            logger.LogInformation("Diagnostics requested for analyzer {AnalyzerId}", incoming.AnalyzerId);
            return new DiagnosticsResponse("Diagnostics request sent successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to request diagnostics for analyzer {AnalyzerId}", incoming.AnalyzerId);
            return new DiagnosticsResponse("Failed to request diagnostics");
        }
    }
}