using Flights.Infrastructure.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Lambda.Endpoints;

internal sealed class FlightsSummaryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights/summary", InvokeAsync)
              .WithSummary("Get a summary of flights")
              .Produces<FlightsSummaryDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Ok<FlightsSummaryDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx, CancellationToken ct)
    {
        var aircraftCount = await ctx.Aircraft.CountAsync(ct);
        var airportCount = await ctx.Airports.CountAsync(ct);
        var flightCount = await ctx.Flights.CountAsync(ct);
        var summary = new FlightsSummaryDto(aircraftCount, airportCount, flightCount);
        return TypedResults.Ok(summary);
    }
}
