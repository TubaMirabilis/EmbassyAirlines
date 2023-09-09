using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Mapping;
using EmbassyAirlines.Application.Repositories;
using Mediator;

namespace EmbassyAirlines.Application.CommandHandlers;

public sealed class NewAircraftCommandHandler : ICommandHandler<NewAircraftDto, AircraftDto>
{
    private readonly IFleetRepository _repository;
    public NewAircraftCommandHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<AircraftDto> Handle(NewAircraftDto command, CancellationToken cancellationToken)
    {
        var mapper = new AircraftMapper();
        var aircraft = mapper.MapNewAircraftDtoToAircraft(command);
        await _repository.AddAircraftAsync(aircraft, cancellationToken);
        return mapper.MapAircraftToAircraftDto(aircraft);
    }
}