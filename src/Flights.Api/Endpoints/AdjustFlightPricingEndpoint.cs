using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Extensions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class AdjustFlightPricingEndpoint : IEndpoint
{
    private readonly IBus _bus;
    private readonly ILogger<AdjustFlightPricingEndpoint> _logger;
    public AdjustFlightPricingEndpoint(IBus bus, ILogger<AdjustFlightPricingEndpoint> logger)
    {
        _bus = bus;
        _logger = logger;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/pricing", InvokeAsync);
    private async Task<IResult> InvokeAsync(ApplicationDbContext ctx, Guid id, AdjustFlightPricingDto dto, CancellationToken ct)
    {
        var flight = await ctx.Flights
                              .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (flight is null)
        {
            _logger.LogWarning("Flight with ID {Id} not found", id);
            var error = Error.NotFound("Flight.NotFound", $"Flight with ID {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var economyPrice = new Money(dto.EconomyPrice);
        var businessPrice = new Money(dto.BusinessPrice);
        flight.AdjustPricing(economyPrice, businessPrice);
        await ctx.SaveChangesAsync(ct);
        await _bus.Publish(new FlightPricingAdjustedEvent(flight.Id, economyPrice.Amount, businessPrice.Amount), ct);
        return TypedResults.Ok(flight.ToDto());
    }
}
