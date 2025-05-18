using Api.Http.Models;
using FluentValidation;

namespace Api.Http.Validators;

public class InitiateLoginRequestValidator : AbstractValidator<InitiateLoginRequest>
{
    public InitiateLoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

public class LoginVerificationRequestValidator : AbstractValidator<LoginVerificationRequest>
{
    public LoginVerificationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
            
        RuleFor(x => x.Code)
            .InclusiveBetween(100000, 999999)
            .WithMessage("Code must be a 6-digit number");
    }
}