using Aircraft.Core.Models;
using FluentValidation;
using Shared.Contracts;

namespace Aircraft.Api.Lambda;

internal sealed class CreateAircraftDtoValidator : AbstractValidator<CreateAircraftDto>
{
    public CreateAircraftDtoValidator()
    {
        When(x => x.Status == Status.Parked.ToString(), () =>
        {
            RuleFor(x => x.ParkedAt)
                .NotEmpty()
                .WithMessage("ParkedAt must be provided when status is Parked.")
                .MaximumLength(4)
                .WithMessage("ParkedAt must be no longer than 4 characters.");
            RuleFor(x => x.EnRouteTo)
                .Empty()
                .WithMessage("EnRouteTo must be empty when status is Parked.");
        });
        When(x => x.Status == Status.EnRoute.ToString(), () =>
        {
            RuleFor(x => x.EnRouteTo)
                .NotEmpty()
                .WithMessage("EnRouteTo must be provided when status is EnRoute.")
                .MaximumLength(4)
                .WithMessage("EnRouteTo must be no longer than 4 characters.");
            RuleFor(x => x.ParkedAt)
                .Empty()
                .WithMessage("ParkedAt must be empty when status is EnRoute.");
        });
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
