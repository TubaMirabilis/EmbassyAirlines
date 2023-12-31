using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Mapping;
using EmbassyAirlines.Application.Repositories;
using ErrorOr;
using Mediator;

namespace EmbassyAirlines.Application.CommandHandlers;

public sealed class NewAircraftCommandHandler : ICommandHandler<NewAircraftDto, ErrorOr<AircraftDto>>
{
    private readonly IFleetRepository _repository;
    public NewAircraftCommandHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<ErrorOr<AircraftDto>> Handle(NewAircraftDto command, CancellationToken ct)
    {
        var mapper = new AircraftMapper();
        var aircraft = mapper.MapNewAircraftDtoToAircraft(command);
        await _repository.AddAircraftAsync(aircraft, ct);
        return mapper.MapAircraftToAircraftDto(aircraft);
    }
}