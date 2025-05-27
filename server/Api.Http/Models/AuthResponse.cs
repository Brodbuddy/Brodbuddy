namespace Api.Http.Models;

public record TestTokenResponse(string AccessToken);
public record LoginVerificationResponse(string AccessToken);
public record RefreshTokenResponse(string AccessToken);
public record UserInfoResponse(Guid UserId, string Email, bool IsAdmin);