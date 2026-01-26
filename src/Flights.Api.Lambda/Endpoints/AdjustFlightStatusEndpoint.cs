using AWS.Messaging;
using ErrorOr;
using Flights.Api.Lambda.Extensions;
using Flights.Core.Models;
using Flights.Infrastructure.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Shared;
using Shared.Abstractions;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Lambda.Endpoints;

internal sealed class AdjustFlightStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/status", InvokeAsync)
              .WithSummary("Adjust the status of an existing flight")
              .Accepts<AdjustFlightStatusDto>("application/json")
              .Produces<FlightDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status400BadRequest)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Ok<FlightDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx,
                                                                                     IClock clock,
                                                                                     ILogger<AdjustFlightStatusEndpoint> logger,
                                                                                     IMessagePublisher publisher,
                                                                                     Guid id,
                                                                                     AdjustFlightStatusDto dto,
                                                                                     CancellationToken ct)
    {
        if (!Enum.TryParse<FlightStatus>(dto.Status, out var newStatus))
        {
            logger.LogWarning("Invalid flight status: {Status}", dto.Status);
            var error = Error.Validation("Flight.InvalidStatus", $"Invalid flight status: {dto.Status}");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var flight = await ctx.Flights
                              .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (flight is null)
        {
            logger.LogWarning("Flight with ID {Id} not found", id);
            var error = Error.NotFound("Flight.NotFound", $"Flight with ID {id} not found");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        try
        {
            flight.AdjustStatus(newStatus, clock.GetCurrentInstant());
            await ctx.SaveChangesAsync(ct);
            logger.LogInformation("Adjusted status for flight {Id}: New Status - {NewStatus}", id, newStatus);
            IFlightStatusManagementEvent message = newStatus switch
            {
                FlightStatus.Cancelled => new FlightCancelledEvent(Guid.NewGuid(), flight.Id),
                FlightStatus.Arrived => new FlightArrivedEvent(Guid.NewGuid(), flight.Aircraft.Id, flight.Id, flight.ArrivalAirport.IcaoCode),
                FlightStatus.Delayed => new FlightDelayedEvent(Guid.NewGuid(), flight.Id),
                FlightStatus.DelayedEnRoute => new FlightMarkedAsDelayedEnRouteEvent(Guid.NewGuid(), flight.Aircraft.Id, flight.Id, flight.ArrivalAirport.IcaoCode),
                FlightStatus.EnRoute => new FlightMarkedAsEnRouteEvent(Guid.NewGuid(), flight.Aircraft.Id, flight.Id, flight.ArrivalAirport.IcaoCode),
                _ => throw new InvalidOperationException("No event defined for the given flight status.")
            };
            await publisher.PublishAsync(message, ct);
            return TypedResults.Ok(flight.ToDto());
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid status transition from {From} to {To}", flight.Status, newStatus);
            var error = Error.Validation("Flight.InvalidStatusTransition", ex.Message);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
    }
}
