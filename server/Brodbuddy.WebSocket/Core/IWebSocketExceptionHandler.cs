using System.Text.Json;
using Fleck;
using Microsoft.Extensions.Logging;

namespace Brodbuddy.WebSocket.Core;

public record WebSocketError(string Code, string Message);

public static class WebSocketErrorCodes
{
    public const string InvalidMessage = "INVALID_MESSAGE";
    public const string MissingFields = "MISSING_FIELDS";
    public const string OperationError = "OPERATION_ERROR";
    public const string ConnectionError = "CONNECTION_ERROR";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string UnknownMessage = "UNKNOWN_MESSAGE";
    public const string InternalError = "INTERNAL_ERROR";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
}


public interface IWebSocketExceptionHandler
{
    WebSocketError HandleException(Exception ex);
}

public class WebSocketExceptionHandler(ILogger<WebSocketExceptionHandler> logger) : IWebSocketExceptionHandler
{
    public virtual WebSocketError HandleException(Exception ex)
    {
        logger.LogError(ex, "WebSocket error occured");
        
        return ex switch
        {
            JsonException             => new WebSocketError(WebSocketErrorCodes.InvalidMessage, "Invalid message format"),
            KeyNotFoundException      => new WebSocketError(WebSocketErrorCodes.MissingFields, "Message missing required fields"),
            InvalidOperationException => new WebSocketError(WebSocketErrorCodes.OperationError, ex.Message),
            WebSocketException        => new WebSocketError(WebSocketErrorCodes.ConnectionError, "WebSocket connection error"),
            _                         => new WebSocketError(WebSocketErrorCodes.InternalError, "An unexpected error occurred")
        };
    }
}