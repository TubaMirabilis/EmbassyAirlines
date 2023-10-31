using EaCommon.Interfaces;
using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Validators;
using ErrorOr;
using FluentValidation.Results;
using Mediator;

namespace EmbassyAirlines.Application.Commands;

public sealed record UpdateAircraft(Guid Id, UpdateAircraftDto Dto) : ICommand<ErrorOr<AircraftDto>>, IValidate
{
    public async Task<ValidationResult> ValidateAsync(CancellationToken ct)
            => await new UpdateAircraftValidator().ValidateAsync(this, ct);
}