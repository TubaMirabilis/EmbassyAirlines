using FluentValidation;

namespace Flights.Api;

internal sealed class CreateOrUpdateFlightDtoValidator : AbstractValidator<CreateOrUpdateFlightDto>
{
    public CreateOrUpdateFlightDtoValidator()
    {
        RuleFor(x => x.AircraftId)
            .NotEmpty()
                .WithMessage("Aircraft id is required.");
        RuleFor(x => x.FlightNumberIcao)
            .MaximumLength(7)
                .WithMessage("ICAO flight number must be 7 characters or less.")
            .Matches("^[A-Z0-9]+$")
                .WithMessage("ICAO flight number must be alphanumeric.");
        RuleFor(x => x.FlightNumberIata)
            .MaximumLength(6)
                .WithMessage("IATA flight number must be 6 characters or less.")
            .Matches("^[A-Z0-9]+$")
                .WithMessage("IATA flight number must be alphanumeric.");
        RuleFor(x => x.DepartureAirportId)
            .NotEmpty()
                .WithMessage("Departure airport id is required.");
        RuleFor(x => x.ArrivalAirportId)
            .NotEmpty()
                .WithMessage("Arrival airport id is required.");
        RuleFor(x => x.DepartureLocalTime)
            .NotEmpty()
                .WithMessage("Departure time is required.");
        RuleFor(x => x.ArrivalLocalTime)
            .NotEmpty()
                .WithMessage("Arrival time is required.");
        RuleFor(x => x.EconomyPrice)
            .GreaterThan(0)
                .WithMessage("Economy price must be greater than 0.");
        RuleFor(x => x.BusinessPrice)
            .GreaterThan(0)
                .WithMessage("Business price must be greater than 0.");
    }
}
