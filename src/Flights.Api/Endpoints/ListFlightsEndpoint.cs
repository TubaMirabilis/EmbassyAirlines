using Flights.Api.Database;
using Microsoft.EntityFrameworkCore;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class ListFlightsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights", InvokeAsync);
    private static async Task<IResult> InvokeAsync(ApplicationDbContext ctx, CancellationToken ct)
    {
        var flights = await ctx.Flights.ToListAsync(ct);
        var list = flights.Select(f => f.ToDto()).ToList();
        return TypedResults.Ok(list);
    }
}
