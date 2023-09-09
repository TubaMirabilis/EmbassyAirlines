using FluentValidation;
using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Enums;

namespace EmbassyAirlines.Application.Validators;

public class UpdateAircraftDtoValidator : AbstractValidator<UpdateAircraftDto>
{
    public UpdateAircraftDtoValidator()
    {
        RuleFor(dto => dto.Registration)
            .MaximumLength(16).WithMessage("Aircraft registration must not exceed 16 characters.")
            .NotEmpty().WithMessage("Aircraft registration is required.")
            .NotNull().WithMessage("Aircraft registration is required.");
        RuleFor(dto => dto.Model)
            .MaximumLength(64).WithMessage("Aircraft model must not exceed 64 characters.")
            .NotEmpty().WithMessage("Aircraft model is required.")
            .NotNull().WithMessage("Aircraft model is required.");
        RuleFor(dto => dto.Type)
            .IsEnumName(typeof(AircraftType))
            .WithMessage("Aircraft type must be a valid aircraft type.");
        RuleFor(dto => dto.EconomySeats)
            .GreaterThanOrEqualTo(0).WithMessage("Aircraft economy seats must be greater than or equal to 0.")
            .NotNull().WithMessage("Aircraft economy seats is required.");
        RuleFor(dto => dto.BusinessSeats)
            .GreaterThanOrEqualTo(0).WithMessage("Aircraft business seats must be greater than or equal to 0.")
            .NotNull().WithMessage("Aircraft business seats is required.");
        RuleFor(dto => dto.FlightHours)
            .GreaterThanOrEqualTo(0).WithMessage("Aircraft flight hours must be greater than or equal to 0.")
            .NotNull().WithMessage("Aircraft flight hours is required.");
        RuleFor(dto => dto.BasicEmptyWeight)
            .GreaterThanOrEqualTo(0).WithMessage("Aircraft basic empty weight must be greater than or equal to 0.")
            .NotNull().WithMessage("Aircraft basic empty weight is required.");
        RuleFor(dto => dto.MaximumZeroFuelWeight)
            .GreaterThanOrEqualTo(0).WithMessage("Aircraft maximum zero fuel weight must be greater than or equal to 0.")
            .NotNull().WithMessage("Aircraft maximum zero fuel weight is required.");
        RuleFor(dto => dto.MaximumTakeoffWeight)
            .GreaterThanOrEqualTo(0).WithMessage("Aircraft maximum takeoff weight must be greater than or equal to 0.")
            .NotNull().WithMessage("Aircraft maximum takeoff weight is required.");
        RuleFor(dto => dto.MaximumLandingWeight)
            .GreaterThanOrEqualTo(0).WithMessage("Aircraft maximum landing weight must be greater than or equal to 0.")
            .NotNull().WithMessage("Aircraft maximum landing weight is required.");
        RuleFor(dto => dto.MaximumCargoWeight)
            .GreaterThanOrEqualTo(0).WithMessage("Aircraft maximum cargo weight must be greater than or equal to 0.")
            .NotNull().WithMessage("Aircraft maximum cargo weight is required.");
        RuleFor(dto => dto.FuelOnboard)
            .GreaterThanOrEqualTo(0).WithMessage("Aircraft fuel onboard must be greater than or equal to 0.")
            .NotNull().WithMessage("Aircraft fuel onboard is required.");
        RuleFor(dto => dto.FuelCapacity)
            .GreaterThanOrEqualTo(0).WithMessage("Aircraft fuel capacity must be greater than or equal to 0.")
            .NotNull().WithMessage("Aircraft fuel capacity is required.");
        RuleFor(dto => dto.MinimumCabinCrew)
            .GreaterThanOrEqualTo(0).WithMessage("Aircraft minimum cabin crew must be greater than or equal to 0.")
            .NotNull().WithMessage("Aircraft minimum cabin crew is required.");
    }
}