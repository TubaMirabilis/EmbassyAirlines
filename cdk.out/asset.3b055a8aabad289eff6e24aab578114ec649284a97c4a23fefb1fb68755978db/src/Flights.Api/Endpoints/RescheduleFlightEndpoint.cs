using AWS.Messaging;
using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class RescheduleFlightEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/schedule", InvokeAsync);
    private static async Task<IResult> InvokeAsync(ApplicationDbContext ctx,
                                                   ILogger<RescheduleFlightEndpoint> logger,
                                                   IMessagePublisher publisher,
                                                   Guid id,
                                                   RescheduleFlightDto dto,
                                                   CancellationToken ct)
    {
        var flight = await ctx.Flights
                              .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (flight is null)
        {
            logger.LogWarning("Flight with ID {Id} not found", id);
            var error = Error.NotFound("Flight.NotFound", $"Flight with ID {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        if (!Enum.TryParse<SchedulingAmbiguityPolicy>(dto.SchedulingAmbiguityPolicy, out var schedulingAmbiguityPolicy))
        {
            logger.LogWarning("Invalid scheduling ambiguity policy: {Policy}", dto.SchedulingAmbiguityPolicy);
            var error = Error.Validation("Flight.InvalidSchedulingAmbiguityPolicy", "Invalid scheduling ambiguity policy");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        try
        {
            flight.Reschedule(dto.DepartureLocalTime, dto.ArrivalLocalTime, schedulingAmbiguityPolicy);
            await ctx.SaveChangesAsync(ct);
            await publisher.PublishAsync(new FlightRescheduledEvent(id, dto.DepartureLocalTime, dto.ArrivalLocalTime), ct);
            return TypedResults.Ok(flight.ToDto());
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning(ex, "Invalid operation while rescheduling flight: {Message}", ex.Message);
            var error = Error.Validation("Flight.ReschedulingFailed", ex.Message);
            return ErrorHandlingHelper.HandleProblem(error);
        }
    }
}
