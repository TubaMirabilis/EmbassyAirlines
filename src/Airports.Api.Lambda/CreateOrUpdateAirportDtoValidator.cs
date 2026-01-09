using FluentValidation;
using Shared.Contracts;

namespace Airports.Api.Lambda;

internal sealed class CreateOrUpdateAirportDtoValidator : AbstractValidator<CreateOrUpdateAirportDto>
{
    public CreateOrUpdateAirportDtoValidator()
    {
        RuleFor(x => x.IcaoCode)
            .Matches("^[A-Z]{4}$")
                .WithMessage("ICAO Code must consist of 4 uppercase letters only.");
        RuleFor(x => x.IataCode)
            .Matches("^[A-Z]{3}$")
                .WithMessage("IATA Code must consist of 3 uppercase letters only.");
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithMessage("Name is required.")
            .MaximumLength(100)
                .WithMessage("Name must not exceed 100 characters in length.");
        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
                .WithMessage("Time zone is required.")
            .MaximumLength(100)
                .WithMessage("Time zone must not exceed 100 characters in length.");
    }
}
