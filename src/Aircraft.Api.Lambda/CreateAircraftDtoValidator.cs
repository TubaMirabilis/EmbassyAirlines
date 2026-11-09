using FluentValidation;
using Shared.Contracts;

namespace Aircraft.Api.Lambda;

internal sealed class CreateAircraftDtoValidator : AbstractValidator<CreateAircraftDto>
{
    public CreateAircraftDtoValidator()
    {
        RuleFor(x => x.TailNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Matches(@"^[A-Z0-9-]+$")
            .WithMessage("Tail number must only contain uppercase letters, numbers, and dashes.")
            .MaximumLength(12);
        RuleFor(x => x.EquipmentCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Matches(@"^[A-Z0-9]+$")
            .WithMessage("Equipment code must only contain uppercase letters and numbers.")
            .MaximumLength(4);
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
        RuleFor(x => x.Situation)
            .NotNull()
            .SetValidator(new AircraftSituationDtoValidator());
    }
}
