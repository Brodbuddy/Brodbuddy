using Brodbuddy.WebSocket.Core;
using Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace Api.Websocket.ExceptionHandler;

public class GlobalWebsocketExceptionHandler(ILogger<GlobalWebsocketExceptionHandler> logger) : WebSocketExceptionHandler(logger)
{
    public override WebSocketError HandleException(Exception ex)
    {
        return ex switch
        {
            BusinessRuleViolationException businessRule => new WebSocketError(WebSocketErrorCodes.ValidationError, $"Business rule violated: {businessRule.RuleName}"),
            EntityNotFoundException entityNotFound => new WebSocketError(WebSocketErrorCodes.ValidationError, $"Entity not found: {entityNotFound.EntityName} with ID {entityNotFound.EntityId}"),
            AuthenticationException authEx => new WebSocketError(WebSocketErrorCodes.Unauthorized, $"Authentication failed: {authEx.FailureReason}"),
            AuthorizationException authzEx => new WebSocketError(WebSocketErrorCodes.Forbidden, $"Access denied. Required permission: {authzEx.RequiredPermission}"),
            _ => base.HandleException(ex)
        };
    }
}