using Flights.Api.Lambda.Database;
using MassTransit;
using Shared.Contracts;

namespace Flights.Api.Lambda;

internal sealed class AirportDeletedConsumer : IConsumer<AirportDeletedEvent>
{
    private readonly ApplicationDbContext _context;
    public AirportDeletedConsumer(ApplicationDbContext context) => _context = context;
    public async Task Consume(ConsumeContext<AirportDeletedEvent> context)
    {
        var id = context.Message.Id;
        var airport = await _context.Airports
                                    .FindAsync(id);
        if (airport is not null)
        {
            _context.Remove(airport);
            await _context.SaveChangesAsync();
        }
    }
}
