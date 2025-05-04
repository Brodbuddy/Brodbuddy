namespace Api.Http.Models;

public record TestTokenResponse(string AccessToken);
public record UserInfoResponse(string Email, bool IsAdmin);