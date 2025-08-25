using Flights.Api.Database;
using MassTransit;
using Shared.Contracts;

namespace Flights.Api;

internal sealed class AircraftCreatedConsumer : IConsumer<AircraftCreatedEvent>
{
    private readonly ApplicationDbContext _ctx;
    private readonly ILogger<AircraftCreatedConsumer> _logger;
    public AircraftCreatedConsumer(ApplicationDbContext ctx, ILogger<AircraftCreatedConsumer> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }
    public async Task Consume(ConsumeContext<AircraftCreatedEvent> context)
    {
        _logger.LogInformation("Consuming AircraftCreatedEvent for aircraft with ID {Id}", context.Message.Id);
        var aircraft = Aircraft.Create(context.Message.Id, context.Message.TailNumber, context.Message.EquipmentCode);
        _ctx.Aircraft.Add(aircraft);
        await _ctx.SaveChangesAsync();
    }
}
