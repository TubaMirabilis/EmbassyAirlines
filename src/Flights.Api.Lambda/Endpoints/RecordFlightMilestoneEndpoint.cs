using ErrorOr;
using Flights.Api.Lambda.Extensions;
using Flights.Core.Models;
using Flights.Infrastructure.Database;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;
using Shared.Contracts;
using Shared.HttpApiLambdaDefaults;

namespace Flights.Api.Lambda.Endpoints;

internal sealed class RecordFlightMilestoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/milestones", InvokeAsync)
              .WithSummary("Record a milestone for an existing flight")
              .Accepts<RecordFlightMilestoneDto>("application/json")
              .Produces<FlightDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status400BadRequest)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status409Conflict)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Ok<FlightDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx,
                                                                                     IClock clock,
                                                                                     ILogger<RecordFlightMilestoneEndpoint> logger,
                                                                                     Guid id,
                                                                                     RecordFlightMilestoneDto dto,
                                                                                     CancellationToken ct)
    {
        if (!Enum.TryParse<FlightMilestone>(dto.Milestone, ignoreCase: true, out var newMilestone) || !Enum.IsDefined(newMilestone))
        {
            logger.LogWarning("Invalid flight milestone: {Milestone}", dto.Milestone);
            var error = Error.Validation("Flight.InvalidMilestone", $"Invalid flight milestone: {dto.Milestone}");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var occurredAt = dto.OccurredAt.ToInstant();
        var now = clock.GetCurrentInstant();
        var tolerance = Duration.FromMinutes(5);
        var futureLimit = now.Plus(tolerance);
        if (occurredAt > futureLimit)
        {
            logger.LogWarning("The request submitted a timestamp which violates the future tolerance limit.");
            var error = Error.Validation("Flight.MilestoneViolatesFutureTolerance", "The request submitted a timestamp which violates the future tolerance limit.");
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
            flight.RecordMilestone(newMilestone, occurredAt, now);
            await ctx.SaveChangesAsync(ct);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Recorded milestone for flight {Id}: New Milestone - {NewMilestone}", id, newMilestone);
            }
            return TypedResults.Ok(flight.ToDto());
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            logger.LogWarning(ex, "Failed to record the milestone");
            var error = Error.Validation("Flight.FailedToRecordMilestone", ex.Message);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
    }
}
