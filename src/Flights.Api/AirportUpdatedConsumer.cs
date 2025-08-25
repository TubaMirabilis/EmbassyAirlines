using Flights.Api.Database;
using MassTransit;
using Shared.Contracts;

namespace Flights.Api;

internal sealed class AirportUpdatedConsumer : IConsumer<AirportUpdatedEvent>
{
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<AirportUpdatedConsumer> _logger;
    public AirportUpdatedConsumer(ApplicationDbContext ctx, ILogger<AirportUpdatedConsumer> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }
    public async Task Consume(ConsumeContext<AirportUpdatedEvent> context)
    {
        _logger.LogInformation("Consuming AirportUpdatedEvent for airport with ID {Id}", context.Message.Id);
        var airport = await _ctx.Airports.FindAsync(context.Message.Id);
        if (airport is null)
        {
            _logger.LogWarning("Airport with ID {Id} not found", context.Message.Id);
            return;
        }
        airport.Update(context.Message.IcaoCode, context.Message.IataCode, context.Message.Name, context.Message.TimeZoneId);
        await _ctx.SaveChangesAsync();
    }
}
