using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Exceptions;
using EmbassyAirlines.Application.Mapping;
using EmbassyAirlines.Application.Queries;
using EmbassyAirlines.Application.Repositories;
using EmbassyAirlines.Domain;
using Mediator;

namespace EmbassyAirlines.Application.QueryHandlers;

public sealed class GetFleetHandler : IQueryHandler<GetFleet, IEnumerable<AircraftDto>>
{
    private readonly IFleetRepository _repository;
    public GetFleetHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<IEnumerable<AircraftDto>> Handle(GetFleet query, CancellationToken cancellationToken)
    {
        var mapper = new AircraftMapper();
        var aircraft = await _repository.GetFleetAsync(cancellationToken);
        return aircraft.Select(a => mapper.MapAircraftToAircraftDto(a));
    }
}