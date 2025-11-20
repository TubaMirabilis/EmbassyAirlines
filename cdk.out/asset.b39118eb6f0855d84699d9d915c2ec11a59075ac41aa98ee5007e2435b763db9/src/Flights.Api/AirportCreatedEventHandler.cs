using AWS.Messaging;
using Flights.Api.Database;
using Shared.Contracts;

namespace Flights.Api;

internal sealed class AirportCreatedEventHandler : IMessageHandler<AirportCreatedEvent>
{
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<AirportCreatedEventHandler> _logger;
    public AirportCreatedEventHandler(ApplicationDbContext ctx, ILogger<AirportCreatedEventHandler> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }
    public async Task<MessageProcessStatus> HandleAsync(MessageEnvelope<AirportCreatedEvent> messageEnvelope, CancellationToken token = default)
    {
        _logger.LogInformation("Consuming AirportCreatedEvent for airport with ID {Id}", messageEnvelope.Message.Id);
        var args = new AirportCreationArgs
        {
            IataCode = messageEnvelope.Message.IataCode,
            IcaoCode = messageEnvelope.Message.IcaoCode,
            Id = messageEnvelope.Message.Id,
            Name = messageEnvelope.Message.Name,
            TimeZoneId = messageEnvelope.Message.TimeZoneId
        };
        var airport = Airport.Create(args);
        _ctx.Airports.Add(airport);
        await _ctx.SaveChangesAsync(token);
        return MessageProcessStatus.Success();
    }
}
