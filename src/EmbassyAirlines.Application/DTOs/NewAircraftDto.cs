using EaCommon.Interfaces;
using EmbassyAirlines.Application.Validators;
using FluentResults;
using Mediator;

namespace EmbassyAirlines.Application.Dtos;

public sealed record NewAircraftDto(string Registration, string Model,
    string Type, int EconomySeats, int BusinessSeats, float FlightHours,
    int BasicEmptyWeight, int MaximumZeroFuelWeight, int MaximumTakeoffWeight,
    int MaximumLandingWeight, int MaximumCargoWeight, int FuelOnboard,
    int FuelCapacity, int MinimumCabinCrew) : ICommand<AircraftDto>, IValidate
{
    public Result Validate()
    {
        var validator = new NewAircraftDtoValidator();
        var result = validator.Validate(this);
        if (result.IsValid)
        {
            return Result.Ok();
        }
        return Result.Fail(result.Errors.Select(e => e.ErrorMessage));
    }
}