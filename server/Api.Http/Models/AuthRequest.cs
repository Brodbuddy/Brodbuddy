namespace Api.Http.Models;

public record InitiateLoginRequest(string Email);
public record LoginVerificationRequest(string Email, int Code);