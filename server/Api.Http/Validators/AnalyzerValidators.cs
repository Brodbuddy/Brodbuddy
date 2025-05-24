using System.Text.RegularExpressions;
using Api.Http.Models;
using FluentValidation;

namespace Api.Http.Validators;

public class RegisterAnalyzerRequestValidator : AbstractValidator<RegisterAnalyzerRequest>
{
    public RegisterAnalyzerRequestValidator()
    {
        RuleFor(x => x.ActivationCode)
            .NotEmpty().WithMessage("Activation code is required")
            .Custom((code, context) =>
            {
                var withDashes = Regex.IsMatch(code, "^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$", RegexOptions.IgnoreCase);
                var withoutDashes = Regex.IsMatch(code, "^[A-Z0-9]{12}$", RegexOptions.IgnoreCase);
                
                if (!withDashes && !withoutDashes)
                {
                    context.AddFailure("Invalid activation code format. Must be 12 characters (e.g., XXXXXXXXXXXX) or formatted with dashes (e.g., XXXX-XXXX-XXXX)");
                }
            });
            
        RuleFor(x => x.Nickname)
            .MaximumLength(255).WithMessage("Nickname must not exceed 255 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Nickname));
    }
}

public class CreateAnalyzerRequestValidator : AbstractValidator<CreateAnalyzerRequest>
{
    public CreateAnalyzerRequestValidator()
    {
        RuleFor(x => x.MacAddress)
            .NotEmpty().WithMessage("MAC address is required")
            .Matches("^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$")
            .WithMessage("Invalid MAC address format");
            
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters");
    }
}