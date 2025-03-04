using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Extensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class GetFlightById
{
    public sealed record Query(Guid Id) : IQuery<ErrorOr<FlightDto>>;
    public sealed class Handler : IQueryHandler<Query, ErrorOr<FlightDto>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx) => _ctx = ctx;
        public async ValueTask<ErrorOr<FlightDto>> Handle(Query query, CancellationToken cancellationToken)
        {
            var flight = await _ctx.Flights
                                   .AsNoTracking()
                                   .SingleOrDefaultAsync(a => a.Id == query.Id, cancellationToken);
            return flight is not null
                ? flight.ToDto()
                : Error.NotFound("Query.NotFound", $"Flight with id {query.Id} was not found.");
        }
    }
}
public sealed class GetFlightByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights/{id}", GetFlightById)
              .WithName("getFlightById")
              .WithOpenApi();
    private static async Task<IResult> GetFlightById([FromServices] ISender sender, [FromRoute] Guid id, CancellationToken ct)
    {
        var query = new GetFlightById.Query(id);
        var result = await sender.Send(query, ct);
        return result.Match(
            Results.Ok,
            ErrorHandlingHelper.HandleProblems);
    }
}
