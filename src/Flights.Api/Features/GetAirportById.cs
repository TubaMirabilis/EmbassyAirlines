using ErrorOr;
using Flights.Api.Database;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class GetAirportById
{
    public sealed record Query(Guid Id) : IQuery<ErrorOr<AirportDto>>;
    public sealed class Handler : IQueryHandler<Query, ErrorOr<AirportDto>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx) => _ctx = ctx;
        public async ValueTask<ErrorOr<AirportDto>> Handle(Query query, CancellationToken cancellationToken)
        {
            var airport = await _ctx.Airports
                                    .AsNoTracking()
                                    .SingleOrDefaultAsync(a => a.Id == query.Id, cancellationToken);
            return airport is not null
                ? new AirportDto(airport.Id, airport.Name, airport.IataCode, airport.TimeZoneId)
                : Error.NotFound("Query.NotFound", $"Airport with id {query.Id} was not found.");
        }
    }
}
public sealed class GetAirportByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("airports/{id}", GetAirportById)
              .WithName("getAirportById")
              .WithOpenApi();
    private static async Task<IResult> GetAirportById([FromServices] ISender sender, [FromRoute] Guid id, CancellationToken ct)
    {
        var query = new GetAirportById.Query(id);
        var result = await sender.Send(query, ct);
        return result.Match(
            Results.Ok,
            ErrorHandlingHelper.HandleProblems);
    }
}
