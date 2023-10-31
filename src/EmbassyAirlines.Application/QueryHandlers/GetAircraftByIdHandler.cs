using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Mapping;
using EmbassyAirlines.Application.Queries;
using EmbassyAirlines.Application.Repositories;
using EmbassyAirlines.Domain;
using EmbassyAirlines.Domain.DomainErrors;
using ErrorOr;
using Mediator;

namespace EmbassyAirlines.Application.QueryHandlers;

public sealed class GetAircraftByIdHandler : IQueryHandler<GetAircraftById, ErrorOr<AircraftDto>>
{
    private readonly IFleetRepository _repository;
    public GetAircraftByIdHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<ErrorOr<AircraftDto>> Handle(GetAircraftById query, CancellationToken ct)
    {
        var aircraft = await _repository.GetAircraftByIdAsync(query.Id, ct);
        if (aircraft is null)
        {
            return Errors.Aircraft.NotFound;
        }
        return new AircraftMapper().MapAircraftToAircraftDto(aircraft);
    }
}