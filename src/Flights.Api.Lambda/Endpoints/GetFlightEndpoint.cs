using ErrorOr;
using Flights.Api.Lambda.Extensions;
using Flights.Infrastructure.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Lambda.Endpoints;

internal sealed class GetFlightEndpoint : IEndpoint
{
    private readonly ILogger<GetFlightEndpoint> _logger;
    public GetFlightEndpoint(ILogger<GetFlightEndpoint> logger) => _logger = logger;
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights/{id}", InvokeAsync)
              .WithSummary("Get a flight by ID")
              .Produces<FlightDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private async Task<Results<Ok<FlightDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx, Guid id, CancellationToken ct)
    {
        var flight = await ctx.Flights
                              .AsNoTracking()
                              .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (flight is null)
        {
            _logger.LogWarning("Flight with ID {Id} not found", id);
            var error = Error.NotFound("Flight.NotFound", $"Flight with ID {id} not found");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        return TypedResults.Ok(flight.ToDto());
    }
}
