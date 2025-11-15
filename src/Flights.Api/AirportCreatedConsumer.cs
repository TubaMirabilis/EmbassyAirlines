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
        var args = new AirportCreationArgs
        {
            IataCode = context.Message.IataCode,
            IcaoCode = context.Message.IcaoCode,
            Id = context.Message.Id,
            Name = context.Message.Name,
            TimeZoneId = context.Message.TimeZoneId
        };
        var airport = Airport.Create(args);
        _ctx.Airports.Add(airport);
        await _ctx.SaveChangesAsync();
    }
}
