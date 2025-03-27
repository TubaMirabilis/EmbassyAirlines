using Flights.Api.Lambda.Database;
using MassTransit;
using Shared.Contracts;

namespace Flights.Api.Lambda;

internal sealed class AirportCreatedConsumer : IConsumer<AirportCreatedEvent>
{
    private readonly ApplicationDbContext _context;
    public AirportCreatedConsumer(ApplicationDbContext context) => _context = context;
    public async Task Consume(ConsumeContext<AirportCreatedEvent> context)
    {
        var message = context.Message;
        var airport = Airport.Create(message.Id, message.IataCode, message.Name, message.TimeZoneId);
        _context.Add(airport);
        await _context.SaveChangesAsync();
    }
}
