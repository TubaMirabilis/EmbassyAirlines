using AWS.Messaging;
using Flights.Api.Database;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace Flights.Api;

internal sealed class AirportUpdatedEventHandler : IMessageHandler<AirportUpdatedEvent>
{
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<AirportUpdatedEventHandler> _logger;
    public AirportUpdatedEventHandler(ApplicationDbContext ctx, ILogger<AirportUpdatedEventHandler> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }
    public async Task<MessageProcessStatus> HandleAsync(MessageEnvelope<AirportUpdatedEvent> messageEnvelope, CancellationToken token = default)
    {
        _logger.LogInformation("Consuming AirportUpdatedEvent for airport with ID {Id}", messageEnvelope.Message.Id);
        var airport = await _ctx.Airports.SingleOrDefaultAsync(a => a.Id == messageEnvelope.Message.Id, token);
        if (airport is null)
        {
            _logger.LogWarning("Airport with ID {Id} not found", messageEnvelope.Message.Id);
            return MessageProcessStatus.Failed();
        }
        airport.Update(messageEnvelope.Message.IcaoCode, messageEnvelope.Message.IataCode, messageEnvelope.Message.Name, messageEnvelope.Message.TimeZoneId);
        await _ctx.SaveChangesAsync(token);
        return MessageProcessStatus.Success();
    }
}
