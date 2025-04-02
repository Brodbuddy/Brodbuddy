namespace Application.Models;

public class JwtClaims
{
    public required string Role { get; set; }
    public required string Email { get; set; }
    public required string Id { get; set; }
    public required string Exp { get; set; }
}