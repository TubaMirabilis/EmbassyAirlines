using EaCommon.Errors;
using EaCommon.Interfaces;
using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Validators;
using FluentResults;
using Mediator;

namespace EmbassyAirlines.Application.Commands;

public sealed record UpdateAircraft(Guid Id, UpdateAircraftDto Dto) : ICommand<Result<AircraftDto>>, IValidate
{
    public Result Validate()
    {
        var validator = new UpdateAircraftValidator();
        var result = validator.Validate(this);
        if (result.IsValid)
        {
            return Result.Ok();
        }
        return Result.Fail(result.Errors.Select(e => e.ErrorMessage));
    }
}