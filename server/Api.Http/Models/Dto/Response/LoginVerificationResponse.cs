namespace Api.Http.Models.Dto.Response;

public record LoginVerificationResponse(string AccessToken, string RefreshToken);