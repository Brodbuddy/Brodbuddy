using Api.Http.Models;
using FluentValidation;

namespace Api.Http.Validators;

public class UploadFirmwareRequestValidator : AbstractValidator<UploadFirmwareRequest>
{
    public UploadFirmwareRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("Firmware file is required")
            .Must(file => file?.Length > 0).WithMessage("File cannot be empty")
            .Must(file => file?.Length <= 10 * 1024 * 1024).WithMessage("File size cannot exceed 10MB");

        RuleFor(x => x.Version)
            .NotEmpty()
            .Matches(@"^\d+\.\d+\.\d+$").WithMessage("Version must be in format X.Y.Z");

        RuleFor(x => x.Description)
            .MaximumLength(500).When(x => x.Description != null);

        RuleFor(x => x.ReleaseNotes)
            .MaximumLength(5000).When(x => x.ReleaseNotes != null);
    }
}

public class MakeFirmwareAvailableRequestValidator : AbstractValidator<MakeFirmwareAvailableRequest>
{
    public MakeFirmwareAvailableRequestValidator()
    {
        RuleFor(x => x.FirmwareVersionId)
            .NotEmpty();
    }
}