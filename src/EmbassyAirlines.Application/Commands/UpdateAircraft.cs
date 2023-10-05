using System.Diagnostics.CodeAnalysis;
using EaCommon.Errors;
using EaCommon.Interfaces;
using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Validators;
using Mediator;

namespace EmbassyAirlines.Application.Commands;

public sealed record UpdateAircraft(Guid Id, UpdateAircraftDto Dto) : ICommand<AircraftDto>, IValidate
{
    public bool IsValid([NotNullWhen(false)] out ValidationError? error)
    {
        var validator = new UpdateAircraftValidator();
        var result = validator.Validate(this);
        if (result.IsValid)
        {
            error = null;
        }
        else
        {
            error = new ValidationError(result.Errors.Select(e => e.ErrorMessage).ToArray());
        }
        return result.IsValid;
    }
}