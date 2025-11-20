using FluentValidation;

namespace Aircraft.Api.Lambda;

internal sealed class CreateOrUpdateAircraftDtoValidator : AbstractValidator<CreateOrUpdateAircraftDto>
{
    public CreateOrUpdateAircraftDtoValidator()
    {
        RuleFor(x => x.TailNumber)
            .Matches(@"^[A-Z0-9\-]+$")
            .WithMessage("Tail number must only contain uppercase letters, numbers, and dashes.")
            .MaximumLength(12)
            .WithMessage("Tail number must be no longer than 12 characters.");
        RuleFor(x => x.EquipmentCode)
            .Matches("^[A-Z0-9]+$")
            .WithMessage("Equipment code must only contain uppercase letters and numbers.")
            .MaximumLength(4)
            .WithMessage("Equipment code must be no longer than 4 characters.");
        RuleFor(x => x.DryOperatingWeight)
                    .GreaterThan(0)
                    .WithMessage("Dry operating weight must be greater than zero.");
        RuleFor(x => x.MaximumTakeoffWeight)
            .GreaterThan(0)
            .WithMessage("Maximum takeoff weight must be greater than zero.");
        RuleFor(x => x.MaximumLandingWeight)
            .GreaterThan(0)
            .WithMessage("Maximum landing weight must be greater than zero.");
        RuleFor(x => x.MaximumZeroFuelWeight)
            .GreaterThan(0)
            .WithMessage("Maximum zero fuel weight must be greater than zero.");
        RuleFor(x => x.MaximumFuelWeight)
            .GreaterThan(0)
            .WithMessage("Maximum fuel weight must be greater than zero.");
    }
}
