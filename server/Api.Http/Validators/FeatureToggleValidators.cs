using Api.Http.Models;
using FluentValidation;

namespace Api.Http.Validators;

public class FeatureToggleRolloutRequestValidator : AbstractValidator<FeatureToggleRolloutRequest>
{
    public FeatureToggleRolloutRequestValidator()
    {
        RuleFor(x => x.Percentage)
            .InclusiveBetween(0, 100)
            .WithMessage("Percentage must be between 0 and 100");
    }
}