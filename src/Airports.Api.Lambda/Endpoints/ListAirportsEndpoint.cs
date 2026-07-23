using Airports.Infrastructure.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Endpoints;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class ListAirportsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("airports", InvokeAsync)
              .WithSummary("List all airports")
              .Produces<IEnumerable<AirportDto>>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Ok<List<AirportDto>>> InvokeAsync(ApplicationDbContext ctx, CancellationToken ct)
    {
        var res = await ctx.Airports.AsNoTracking().Select(a => new AirportDto(a.Id, a.Name, a.IcaoCode, a.IataCode, a.TimeZoneId)).ToListAsync(ct);
        return TypedResults.Ok(res);
    }
}
