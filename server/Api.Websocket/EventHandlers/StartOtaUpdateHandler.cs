using System.Text.Json.Serialization;
using Api.Websocket.Extensions;
using Application.Services;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Fleck;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Api.Websocket.EventHandlers;

public record StartOtaUpdateRequest(
    [property: JsonPropertyName("userId")] string UserId, 
    [property: JsonPropertyName("analyzerId")] string AnalyzerId, 
    [property: JsonPropertyName("firmwareVersionId")] string FirmwareVersionId);

public record StartOtaUpdateResponse(
    [property: JsonPropertyName("updateId")] Guid UpdateId, 
    [property: JsonPropertyName("status")] string Status);

public class StartOtaUpdateRequestValidator : AbstractValidator<StartOtaUpdateRequest>
{
    public StartOtaUpdateRequestValidator()
    {
        RuleFor(x => x.AnalyzerId)
            .NotEmpty().WithMessage("Analyzer ID is required")
            .MustBeValidGuid();
            
        RuleFor(x => x.FirmwareVersionId)
            .NotEmpty().WithMessage("Firmware version ID is required")
            .MustBeValidGuid();
    }
}

[AllowAnonymous]
public class StartOtaUpdateHandler(IOtaService otaService, ILogger<StartOtaUpdateHandler> logger) : IWebSocketHandler<StartOtaUpdateRequest, StartOtaUpdateResponse>
{
    public async Task<StartOtaUpdateResponse> HandleAsync(StartOtaUpdateRequest incoming, string clientId, IWebSocketConnection socket)
    {
        var analyzerId = Guid.Parse(incoming.AnalyzerId);
        var firmwareVersionId = Guid.Parse(incoming.FirmwareVersionId);
        
        var updateId = await otaService.StartOtaUpdateAsync(analyzerId, firmwareVersionId);
        
        logger.LogInformation("User {UserId} started OTA update {UpdateId} for analyzer {AnalyzerId}", 
            incoming.UserId, updateId, analyzerId);
        
        return new StartOtaUpdateResponse(updateId, "started");
    }
}