using Flights.Api.Database;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class ListFlightsEndpoint : IEndpoint
{
    private readonly ApplicationDbContext _ctx;
    public ListFlightsEndpoint(ApplicationDbContext ctx) => _ctx = ctx;
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights/{id}", InvokeAsync);
    private async Task<IResult> InvokeAsync(CancellationToken ct)
    {
        var flights = await _ctx.Flights.ToListAsync(ct);
        var list = flights.Select(f => f.ToDto()).ToList();
        return TypedResults.Ok(list);
    }
}
