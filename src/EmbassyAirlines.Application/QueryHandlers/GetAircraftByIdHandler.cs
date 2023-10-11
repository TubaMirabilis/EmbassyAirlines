using EaCommon.Errors;
using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Application.Mapping;
using EmbassyAirlines.Application.Queries;
using EmbassyAirlines.Application.Repositories;
using EmbassyAirlines.Domain;
using FluentResults;
using Mediator;

namespace EmbassyAirlines.Application.QueryHandlers;

public sealed class GetAircraftByIdHandler : IQueryHandler<GetAircraftById, Result<AircraftDto>>
{
    private readonly IFleetRepository _repository;
    public GetAircraftByIdHandler(IFleetRepository repository)
    {
        _repository = repository;
    }
    public async ValueTask<Result<AircraftDto>> Handle(GetAircraftById query, CancellationToken ct)
    {
        var aircraft = await _repository.GetAircraftByIdAsync(query.Id, ct);
        if (aircraft is null)
        {
            return Result.Fail(new NotFoundError("Aircraft"));
        }
        return new AircraftMapper().MapAircraftToAircraftDto(aircraft);
    }
}