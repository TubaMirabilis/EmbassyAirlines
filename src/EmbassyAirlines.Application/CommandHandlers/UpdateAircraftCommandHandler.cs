using EmbassyAirlines.Application.Commands;
using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Exceptions;
using EmbassyAirlines.Application.Mapping;
using EmbassyAirlines.Application.Repositories;
using Mediator;

namespace EmbassyAirlines.Application.CommandHandlers;

public sealed class UpdateAircraftCommandHandler : ICommandHandler<UpdateAircraft, AircraftDto>
{
    private readonly IFleetRepository _repository;
    public UpdateAircraftCommandHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<AircraftDto> Handle(UpdateAircraft command, CancellationToken ct)
    {
        var rowsAffected = await _repository.UpdateAircraftAsync(command.Id, command.Dto, ct);
        if (rowsAffected == 0)
        {
            throw new NotFoundException($"Aircraft with id {command.Id} not found");
        }
        var aircraft = await _repository.GetAircraftByIdAsync(command.Id, ct);
        return new AircraftMapper().MapAircraftToAircraftDto(aircraft!);
    }
}