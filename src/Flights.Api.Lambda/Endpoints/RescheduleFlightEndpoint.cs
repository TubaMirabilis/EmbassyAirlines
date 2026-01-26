using AWS.Messaging;
using ErrorOr;
using Flights.Api.Lambda.Extensions;
using Flights.Core.Models;
using Flights.Infrastructure.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Lambda.Endpoints;

internal sealed class RescheduleFlightEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/schedule", InvokeAsync)
              .WithSummary("Reschedule an existing flight")
              .Accepts<RescheduleFlightDto>("application/json")
              .Produces<FlightDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status400BadRequest)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Ok<FlightDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx,
                                                   IClock clock,
                                                   ILogger<RescheduleFlightEndpoint> logger,
                                                   IMessagePublisher publisher,
                                                   Guid id,
                                                   RescheduleFlightDto dto,
                                                   CancellationToken ct)
    {
        if (!Enum.TryParse<SchedulingAmbiguityPolicy>(dto.SchedulingAmbiguityPolicy, out var schedulingAmbiguityPolicy))
        {
            logger.LogWarning("Invalid scheduling ambiguity policy: {Policy}", dto.SchedulingAmbiguityPolicy);
            var error = Error.Validation("Flight.InvalidSchedulingAmbiguityPolicy", "Invalid scheduling ambiguity policy");
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
            var schedule = new FlightSchedule(new FlightScheduleCreationArgs
            {
                DepartureAirport = flight.DepartureAirport,
                DepartureLocalTime = LocalDateTime.FromDateTime(dto.DepartureLocalTime),
                ArrivalAirport = flight.ArrivalAirport,
                ArrivalLocalTime = LocalDateTime.FromDateTime(dto.ArrivalLocalTime),
                Now = clock.GetCurrentInstant(),
                SchedulingAmbiguityPolicy = schedulingAmbiguityPolicy
            });
            flight.Reschedule(schedule, clock.GetCurrentInstant());
            await ctx.SaveChangesAsync(ct);
            logger.LogInformation("Rescheduled flight {Id}: Departure - {DepartureLocalTime}, Arrival - {ArrivalLocalTime}", id, dto.DepartureLocalTime, dto.ArrivalLocalTime);
            await publisher.PublishAsync(new FlightRescheduledEvent(id, dto.DepartureLocalTime, dto.ArrivalLocalTime), ct);
            return TypedResults.Ok(flight.ToDto());
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning(ex, "Invalid operation while rescheduling flight: {Message}", ex.Message);
            var error = Error.Validation("Flight.ReschedulingFailed", ex.Message);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
    }
}
