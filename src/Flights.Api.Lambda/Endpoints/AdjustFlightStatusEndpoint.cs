using AWS.Messaging;
using ErrorOr;
using Flights.Api.Lambda.Extensions;
using Flights.Core.Models;
using Flights.Infrastructure.Database;
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
        => app.MapPatch("flights/{id}/status", InvokeAsync);
    private static async Task<IResult> InvokeAsync(ApplicationDbContext ctx,
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
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var flight = await ctx.Flights
                              .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (flight is null)
        {
            logger.LogWarning("Flight with ID {Id} not found", id);
            var error = Error.NotFound("Flight.NotFound", $"Flight with ID {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        try
        {
            flight.AdjustStatus(newStatus, SystemClock.Instance.GetCurrentInstant());
            await ctx.SaveChangesAsync(ct);
            logger.LogInformation("Adjusted status for flight {Id}: New Status - {NewStatus}", id, newStatus);
            IFlightStatusManagementEvent message = newStatus switch
            {
                FlightStatus.Cancelled => new FlightCancelledEvent(flight.Id),
                FlightStatus.Arrived => new FlightArrivedEvent(flight.Id, flight.ArrivalAirport.IcaoCode),
                FlightStatus.Delayed => new FlightDelayedEvent(flight.Id),
                FlightStatus.DelayedEnRoute => new FlightMarkedAsDelayedEnRouteEvent(flight.Id, flight.ArrivalAirport.IcaoCode),
                FlightStatus.EnRoute => new FlightMarkedAsEnRouteEvent(flight.Id, flight.ArrivalAirport.IcaoCode),
                _ => throw new InvalidOperationException("No event defined for the given flight status.")
            };
            await publisher.PublishAsync(message, ct);
            return TypedResults.Ok(flight.ToDto());
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid status transition from {From} to {To}", flight.Status, newStatus);
            var error = Error.Validation("Flight.InvalidStatusTransition", ex.Message);
            return ErrorHandlingHelper.HandleProblem(error);
        }
    }
}
