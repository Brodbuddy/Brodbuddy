namespace Application.Models;

public record JwtClaims(string Sub, string Iss, string Aud, long Iat, long Exp, string Jti, string Email, string Role);