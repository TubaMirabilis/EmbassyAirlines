using AWS.Messaging;
using Flights.Api.Database;
using Shared.Contracts;

namespace Flights.Api;

internal sealed class AircraftCreatedEventHandler : IMessageHandler<AircraftCreatedEvent>
{
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<AircraftCreatedEventHandler> _logger;
    public AircraftCreatedEventHandler(ApplicationDbContext ctx, ILogger<AircraftCreatedEventHandler> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }
    public async Task<MessageProcessStatus> HandleAsync(MessageEnvelope<AircraftCreatedEvent> messageEnvelope, CancellationToken token = default)
    {
        _logger.LogInformation("Consuming AircraftCreatedEvent for aircraft with ID {Id}", messageEnvelope.Message.Id);
        var aircraft = Aircraft.Create(messageEnvelope.Message.Id, messageEnvelope.Message.TailNumber, messageEnvelope.Message.EquipmentCode);
        _ctx.Aircraft.Add(aircraft);
        await _ctx.SaveChangesAsync(token);
        return MessageProcessStatus.Success();
    }
}
