using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.Http.Models.Dto.Request;

public record LoginVerificationRequest
{
    [Required]
    public string Email { get; init; } = null!;

    [Required]
    [JsonRequired]
    public int Code { get; init; }
}