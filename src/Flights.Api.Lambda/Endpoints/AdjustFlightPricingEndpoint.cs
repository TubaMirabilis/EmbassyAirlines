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

internal sealed class AdjustFlightPricingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/pricing", InvokeAsync)
              .WithSummary("Adjust the pricing of an existing flight")
              .Accepts<AdjustFlightPricingDto>("application/json")
              .Produces<FlightDto>(StatusCodes.Status200OK)
              .ProducesProblem(StatusCodes.Status400BadRequest)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Ok<FlightDto>, ProblemHttpResult>> InvokeAsync(ApplicationDbContext ctx,
                                                                                     IClock clock,
                                                                                     ILogger<AdjustFlightPricingEndpoint> logger,
                                                                                     IMessagePublisher publisher,
                                                                                     Guid id,
                                                                                     AdjustFlightPricingDto dto,
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
        try
        {
            var economyPrice = new Money(dto.EconomyPrice);
            var businessPrice = new Money(dto.BusinessPrice);
            flight.AdjustPricing(economyPrice, businessPrice, clock.GetCurrentInstant());
            await ctx.SaveChangesAsync(ct);
            logger.LogInformation("Adjusted pricing for flight {Id}: Economy - {EconomyPrice}, Business - {BusinessPrice}", id, economyPrice, businessPrice);
            await publisher.PublishAsync(new FlightPricingAdjustedEvent(flight.Id, economyPrice.Amount, businessPrice.Amount), ct);
            return TypedResults.Ok(flight.ToDto());
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning(ex, "Invalid pricing values: Economy - {EconomyPrice}, Business - {BusinessPrice}", dto.EconomyPrice, dto.BusinessPrice);
            var error = Error.Validation("Flight.InvalidPricing", ex.Message);
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
    }
}
