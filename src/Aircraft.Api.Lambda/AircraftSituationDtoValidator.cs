using FluentValidation;
using Shared.Contracts;

namespace Aircraft.Api.Lambda;

internal sealed class AircraftSituationDtoValidator
    : AbstractValidator<AircraftSituationDto>
{
    public AircraftSituationDtoValidator()
    {
        RuleFor(x => x.Status)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(s => AircraftStatusNames.TryParse(s, out _))
            .WithMessage("Status must be Parked or EnRoute.");
        RuleFor(x => x.AirportIcaoCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Matches("^[A-Za-z]{4}$")
            .WithMessage("AirportIcaoCode must contain four letters.");
    }
}
