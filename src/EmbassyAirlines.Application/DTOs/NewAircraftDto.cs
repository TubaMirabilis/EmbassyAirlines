using EaCommon.Interfaces;
using EmbassyAirlines.Application.Validators;
using ErrorOr;
using FluentValidation.Results;
using Mediator;

namespace EmbassyAirlines.Application.Dtos;

public sealed record NewAircraftDto(string Registration, string Model,
    string Type, int EconomySeats, int BusinessSeats, float FlightHours,
    int BasicEmptyWeight, int MaximumZeroFuelWeight, int MaximumTakeoffWeight,
    int MaximumLandingWeight, int MaximumCargoWeight, int FuelOnboard,
    int FuelCapacity, int MinimumCabinCrew) : ICommand<ErrorOr<AircraftDto>>, IValidate
{
public async Task<ValidationResult> ValidateAsync(CancellationToken ct)
        => await new NewAircraftDtoValidator().ValidateAsync(this, ct);
}