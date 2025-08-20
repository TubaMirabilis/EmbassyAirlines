using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Flights.Api;

public sealed class CreateOrUpdateFlightDtoValidator : AbstractValidator<CreateOrUpdateFlightDto>
{
    public CreateOrUpdateFlightDtoValidator()
    {
        RuleFor(x => x.AircraftId)
            .NotEmpty()
                .WithMessage("Aircraft id is required.");
        RuleFor(x => x.FlightNumber)
            .MaximumLength(6)
                .WithMessage("Flight number must be 6 characters or less.")
            .Matches("^[A-Z0-9]+$")
                .WithMessage("Flight number must be alphanumeric.");
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
