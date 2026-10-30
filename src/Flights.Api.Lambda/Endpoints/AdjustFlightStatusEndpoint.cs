using ErrorOr;
using Flights.Api.Lambda.Extensions;
using Flights.Core.Models;
using Flights.Infrastructure.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Shared.Contracts;
using Shared.HttpApiLambdaDefaults;

namespace Flights.Api.Lambda.Endpoints;

internal sealed class AdjustFlightStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/status", InvokeAsync)
              .WithSummary("Mark an existing flight as delayed or cancelled")
              .Accepts<AdjustFlightStatusDto>("application/json")
              .Produces<FlightDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status400BadRequest)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status409Conflict)
              .ProducesProblem(StatusCodes.Status500InternalServerError);

    private static async Task<Results<Ok<FlightDto>, ProblemHttpResult>> InvokeAsync(
        ApplicationDbContext ctx,
        IClock clock,
        ILogger<AdjustFlightStatusEndpoint> logger,
        Guid id,
        AdjustFlightStatusDto dto,
        CancellationToken ct)
    {
        if (!Enum.TryParse<FlightStatus>(
                dto.Status,
                ignoreCase: true,
                out var newStatus) ||
            newStatus is not (FlightStatus.Delayed or FlightStatus.Cancelled))
        {
            logger.LogWarning("Unsupported flight status adjustment: {Status}", dto.Status);
            var error = Error.Validation("Flight.InvalidStatus", "Only Delayed and Cancelled can be set directly. Record operational milestones to transition to EnRoute or Arrived.");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var flight = await ctx.Flights
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (flight is null)
        {
            logger.LogWarning("Flight with ID {Id} not found", id);

            var error = Error.NotFound(
                "Flight.NotFound",
                $"Flight with ID {id} not found");

            return TypedResults.Problem(
                ErrorHandlingHelper.GetProblemDetails(error));
        }

        var previousStatus = flight.Status;

        try
        {
            var occurredAt = clock.GetCurrentInstant();

            switch (newStatus)
            {
                case FlightStatus.Delayed:
                    flight.MarkAsDelayed(occurredAt);
                    break;

                case FlightStatus.Cancelled:
                    flight.Cancel(occurredAt);
                    break;
            }

            await ctx.SaveChangesAsync(ct);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Updated flight {Id} status from {PreviousStatus} to {NewStatus}", id, previousStatus, newStatus);
            }
            return TypedResults.Ok(flight.ToDto());
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(
                ex,
                "Invalid status transition for flight {Id} from {From} to {To}",
                id,
                previousStatus,
                newStatus);

            var error = Error.Conflict(
                "Flight.InvalidStatusTransition",
                ex.Message);

            return TypedResults.Problem(
                ErrorHandlingHelper.GetProblemDetails(error));
        }
    }
}
