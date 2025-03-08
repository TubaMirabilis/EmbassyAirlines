using FluentValidation;

namespace Airports.Api.Lambda;

public sealed class CreateOrUpdateAirportDtoValidator : AbstractValidator<CreateOrUpdateAirportDto>
{
    public CreateOrUpdateAirportDtoValidator()
    {
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