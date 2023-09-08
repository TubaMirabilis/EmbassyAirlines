using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Mapping;
using EmbassyAirlines.Application.Repositories;
using Mediator;

namespace EmbassyAirlines.Application.CommandHandlers;

public sealed class UpdateAircraftCommandHandler : ICommandHandler<UpdateAircraft, AircraftDto>
{
    private readonly IFleetRepository _repository;
    public NewAircraftCommandHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<AircraftDto> Handle(UpdateAircraft command, CancellationToken cancellationToken)
    {
        var aircraft = await _repository.GetAircraftById(command.Id);
        if (aircraft is null)
        {
            throw new Exception($"Aircraft with id {command.Id} not found");
        }
        else
        {
            aircraft.Update(command.Model, command.Registration, command.Capacity);
            await _repository.UpdateAircraft(aircraft);
            return aircraft.ToDto();
        }
    }
}