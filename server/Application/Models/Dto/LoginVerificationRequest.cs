namespace Application.Models.Dto;

public record LoginVerificationRequest(string Email, int Code);