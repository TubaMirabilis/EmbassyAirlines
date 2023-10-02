using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Exceptions;
using EmbassyAirlines.Application.Mapping;
using EmbassyAirlines.Application.Queries;
using EmbassyAirlines.Application.Repositories;
using EmbassyAirlines.Domain;
using Mediator;

namespace EmbassyAirlines.Application.QueryHandlers;

public sealed class GetAircraftByIdHandler : IQueryHandler<GetAircraftById, AircraftDto>
{
    private readonly IFleetRepository _repository;
    public GetAircraftByIdHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<AircraftDto> Handle(GetAircraftById query, CancellationToken ct)
    {
        var aircraft = await _repository.GetAircraftByIdAsync(query.Id, ct);
        if (aircraft is null)
        {
            throw new NotFoundException($"Aircraft with id {query.Id} not found");
        }
        return new AircraftMapper().MapAircraftToAircraftDto(aircraft);
    }
}