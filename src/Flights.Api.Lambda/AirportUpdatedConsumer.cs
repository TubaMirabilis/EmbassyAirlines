using Flights.Api.Lambda.Database;
using MassTransit;
using Shared.Contracts;

namespace Flights.Api.Lambda;

internal sealed class AirportUpdatedConsumer : IConsumer<AirportUpdatedEvent>
{
    private readonly ApplicationDbContext _context;
    public AirportUpdatedConsumer(ApplicationDbContext context) => _context = context;
    public async Task Consume(ConsumeContext<AirportUpdatedEvent> context)
    {
        var message = context.Message;
        var airport = await _context.Airports
                                    .FindAsync(message.Id);
        if (airport is not null)
        {
            airport.Update(message.IataCode, message.Name, message.TimeZoneId);
            await _context.SaveChangesAsync();
        }
    }
}
