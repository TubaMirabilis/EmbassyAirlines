using Flights.Api.Database;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class ListAirports
{
    public sealed record Query : IQuery<IEnumerable<AirportDto>>;
    public sealed class Handler : IQueryHandler<Query, IEnumerable<AirportDto>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx) => _ctx = ctx;
        public async ValueTask<IEnumerable<AirportDto>> Handle(Query query, CancellationToken cancellationToken)
        {
            var airports = await _ctx.Airports
                                     .AsNoTracking()
                                     .ToListAsync(cancellationToken);
            return airports.Select(a => new AirportDto(a.Id, a.Name, a.IataCode, a.TimeZoneId))
                           .ToList();
        }
    }
}
public sealed class ListAirportsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("airports", ListAirports)
              .WithName("listAirports")
              .WithOpenApi();
    private static async Task<IResult> ListAirports([FromServices] ISender sender, CancellationToken ct)
    {
        var query = new ListAirports.Query();
        var result = await sender.Send(query, ct);
        return Results.Ok(result);
    }
}
