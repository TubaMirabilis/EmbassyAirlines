using EmbassyAirlines.Application.Commands;
using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Mapping;
using EmbassyAirlines.Application.Repositories;
using EmbassyAirlines.Domain.DomainErrors;
using ErrorOr;
using Mediator;

namespace EmbassyAirlines.Application.CommandHandlers;

public sealed class UpdateAircraftCommandHandler : ICommandHandler<UpdateAircraft, ErrorOr<AircraftDto>>
{
    private readonly IFleetRepository _repository;
    public UpdateAircraftCommandHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<ErrorOr<AircraftDto>> Handle(UpdateAircraft command, CancellationToken ct)
    {
        var rowsAffected = await _repository.UpdateAircraftAsync(command.Id, command.Dto, ct);
        if (rowsAffected == 0)
        {
            return Errors.Aircraft.NotFound;
        }
        var aircraft = await _repository.GetAircraftByIdAsync(command.Id, ct);
        return new AircraftMapper().MapAircraftToAircraftDto(aircraft!);
    }
}