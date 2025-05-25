namespace Brodbuddy.WebSocket.Auth;

public enum AuthPolicy
{
    /// <summary>
    /// Auth is required for all handlers except those with [AllowAnonymous]
    /// </summary>
    Blacklist,
    
    /// <summary>
    /// Auth is only required for handlers with [Authorize] 
    /// </summary>
    Whitelist
}