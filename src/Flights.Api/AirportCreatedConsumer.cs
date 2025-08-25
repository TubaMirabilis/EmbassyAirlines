using Flights.Api.Database;
using MassTransit;
using Shared.Contracts;

namespace Flights.Api;

internal sealed class AirportCreatedConsumer : IConsumer<AirportCreatedEvent>
{
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<AirportCreatedConsumer> _logger;
    public AirportCreatedConsumer(ApplicationDbContext ctx, ILogger<AirportCreatedConsumer> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }
    public async Task Consume(ConsumeContext<AirportCreatedEvent> context)
    {
        _logger.LogInformation("Consuming AirportCreatedEvent for airport with ID {Id}", context.Message.Id);
        var airport = Airport.Create(context.Message.Id, context.Message.TimeZoneId, context.Message.IataCode, context.Message.IcaoCode, context.Message.Name);
        _ctx.Airports.Add(airport);
        await _ctx.SaveChangesAsync();
    }
}
