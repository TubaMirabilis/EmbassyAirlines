using AWS.Messaging;
using ErrorOr;
using Flights.Api.Lambda.Extensions;
using Flights.Infrastructure.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Lambda.Endpoints;

internal sealed class AssignAircraftToFlightEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/aircraft", InvokeAsync)
              .WithSummary("Assign an aircraft to an existing flight")
              .Accepts<AssignAircraftToFlightDto>("application/json")
              .Produces<FlightDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Ok<FlightDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx,
                                                                                     IClock clock,
                                                                                     ILogger<AssignAircraftToFlightEndpoint> logger,
                                                                                     IMessagePublisher publisher,
                                                                                     Guid id,
                                                                                     AssignAircraftToFlightDto dto,
                                                                                     CancellationToken ct)
    {
        var flight = await ctx.Flights
                              .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (flight is null)
        {
            logger.LogWarning("Flight with ID {Id} not found", id);
            var error = Error.NotFound("Flight.NotFound", $"Flight with ID {id} not found");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var aircraft = await ctx.Aircraft
                                .FirstOrDefaultAsync(a => a.Id == dto.AircraftId, ct);
        if (aircraft is null)
        {
            logger.LogWarning("Aircraft with ID {Id} not found", dto.AircraftId);
            var error = Error.NotFound("Aircraft.NotFound", $"Aircraft with ID {dto.AircraftId} not found");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        flight.AssignAircraft(aircraft, clock.GetCurrentInstant());
        await ctx.SaveChangesAsync(ct);
        logger.LogInformation("Assigned aircraft {AircraftId} to flight {FlightId}", aircraft.Id, flight.Id);
        await publisher.PublishAsync(new AircraftAssignedToFlightEvent(flight.Id, aircraft.Id), ct);
        return TypedResults.Ok(flight.ToDto());
    }
}
