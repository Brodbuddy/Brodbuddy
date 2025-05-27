using FluentValidation;

namespace Api.Websocket.Extensions;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeValidGuid<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(BeValidGuid).WithMessage("Invalid GUID format");
    }
    
    private static bool BeValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }
}