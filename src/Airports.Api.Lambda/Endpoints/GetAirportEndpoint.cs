using Airports.Infrastructure.Database;
using ErrorOr;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Airports.Api.Lambda.Endpoints;

internal sealed class GetAirportEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("airports/{id}", InvokeAsync)
              .WithSummary("Get an airport by ID")
              .Produces<AirportDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Ok<AirportDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx,
                                                                                      ILogger<GetAirportEndpoint> logger,
                                                                                      Guid id,
                                                                                      CancellationToken ct)
    {
        var airport = await ctx.Airports.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (airport is null)
        {
            logger.LogWarning("Airport with ID {Id} not found", id);
            var error = Error.NotFound("Airport.NotFound", $"Airport with ID {id} not found");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        return TypedResults.Ok(new AirportDto(airport.Id, airport.Name, airport.IcaoCode, airport.IataCode, airport.TimeZoneId));
    }
}
