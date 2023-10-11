using EaCommon.Errors;
using EmbassyAirlines.Application.Commands;
using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Mapping;
using EmbassyAirlines.Application.Repositories;
using FluentResults;
using Mediator;

namespace EmbassyAirlines.Application.CommandHandlers;

public sealed class UpdateAircraftCommandHandler : ICommandHandler<UpdateAircraft, Result<AircraftDto>>
{
    private readonly IFleetRepository _repository;
    public UpdateAircraftCommandHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<Result<AircraftDto>> Handle(UpdateAircraft command, CancellationToken ct)
    {
        var rowsAffected = await _repository.UpdateAircraftAsync(command.Id, command.Dto, ct);
        if (rowsAffected == 0)
        {
            return Result.Fail(new NotFoundError("Aircraft"));
        }
        var aircraft = await _repository.GetAircraftByIdAsync(command.Id, ct);
        return new AircraftMapper().MapAircraftToAircraftDto(aircraft!);
    }
}