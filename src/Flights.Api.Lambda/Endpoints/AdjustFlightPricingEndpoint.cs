using AWS.Messaging;
using ErrorOr;
using Flights.Api.Lambda.Extensions;
using Flights.Core.Models;
using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Lambda.Endpoints;

internal sealed class AdjustFlightPricingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/pricing", InvokeAsync);
    private static async Task<IResult> InvokeAsync(ApplicationDbContext ctx,
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
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var economyPrice = new Money(dto.EconomyPrice);
        var businessPrice = new Money(dto.BusinessPrice);
        flight.AdjustPricing(economyPrice, businessPrice, SystemClock.Instance.GetCurrentInstant());
        await ctx.SaveChangesAsync(ct);
        await publisher.PublishAsync(new FlightPricingAdjustedEvent(flight.Id, economyPrice.Amount, businessPrice.Amount), ct);
        return TypedResults.Ok(flight.ToDto());
    }
}
