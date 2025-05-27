using System.Text.Json.Serialization;
using Api.Websocket.Extensions;
using Application.Interfaces.Data.Repositories;
using Brodbuddy.WebSocket.Auth;
using Brodbuddy.WebSocket.Core;
using Brodbuddy.WebSocket.State;
using Core.Entities;
using Core.Messaging;
using Core.ValueObjects;
using Fleck;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Api.Websocket.EventHandlers;
public record MakeFirmwareAvailableRequest([property: JsonPropertyName("firmwareId")] string FirmwareId);
public record MakeFirmwareAvailableResponse([property: JsonPropertyName("firmwareId")] Guid FirmwareId);

public class MakeFirmwareAvailableRequestValidator : AbstractValidator<MakeFirmwareAvailableRequest>
{
    public MakeFirmwareAvailableRequestValidator()
    {
        RuleFor(x => x.FirmwareId)
            .NotEmpty().WithMessage("Firmware ID is required")
            .MustBeValidGuid();
    }
}

[Authorize(Roles = Role.Admin)]
public class MakeFirmwareAvailableHandler(ISocketManager manager, IFirmwareRepository repository, ILogger<MakeFirmwareAvailableHandler> logger) : IWebSocketHandler<MakeFirmwareAvailableRequest, MakeFirmwareAvailableResponse>
{
    public async Task<MakeFirmwareAvailableResponse> HandleAsync(MakeFirmwareAvailableRequest incoming, string clientId, IWebSocketConnection socket)
    {
        var firmwareId = Guid.Parse(incoming.FirmwareId);
        var firmware = await repository.GetByIdAsync(firmwareId);
        
        if (firmware == null) throw new InvalidOperationException($"Firmware not found: {firmwareId}");
        
        var notification = new FirmwareAvailable(
            firmware.Id.ToString(),
            firmware.Version,
            firmware.Description,
            firmware.ReleaseNotes,
            firmware.IsStable,
            firmware.FileSize
        );

        await manager.BroadcastAsync(WebSocketTopics.Everyone.FirmwareAvailable, notification);
        
        logger.LogInformation("Broadcasted firmware availability for version {Version}", firmware.Version);
        
        return new MakeFirmwareAvailableResponse(firmware.Id);
    }
}