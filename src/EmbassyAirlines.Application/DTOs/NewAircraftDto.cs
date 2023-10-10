using System.Diagnostics.CodeAnalysis;
using EaCommon.Errors;
using EaCommon.Interfaces;
using EmbassyAirlines.Application.Validators;
using Mediator;

namespace EmbassyAirlines.Application.Dtos;

public sealed record NewAircraftDto(string Registration, string Model,
    string Type, int EconomySeats, int BusinessSeats, float FlightHours,
    int BasicEmptyWeight, int MaximumZeroFuelWeight, int MaximumTakeoffWeight,
    int MaximumLandingWeight, int MaximumCargoWeight, int FuelOnboard,
    int FuelCapacity, int MinimumCabinCrew) : ICommand<AircraftDto>, IValidate
{
    public bool IsValid([NotNullWhen(false)] out ValidationError? error)
    {
        var validator = new NewAircraftDtoValidator();
        var result = validator.Validate(this);
        if (result.IsValid)
        {
            error = null;
            return result.IsValid;
        }
        error = new ValidationError(result.Errors.Select(e => e.ErrorMessage)
            .ToArray());
        return !result.IsValid;
    }
}