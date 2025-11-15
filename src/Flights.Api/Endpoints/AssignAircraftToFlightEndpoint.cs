using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Extensions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class AssignAircraftToFlightEndpoint : IEndpoint
{
    private readonly IBus _bus;
    private readonly ILogger<AssignAircraftToFlightEndpoint> _logger;
    public AssignAircraftToFlightEndpoint(IBus bus, ILogger<AssignAircraftToFlightEndpoint> logger)
    {
        _bus = bus;
        _logger = logger;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/aircraft", InvokeAsync);
    private async Task<IResult> InvokeAsync(ApplicationDbContext ctx, Guid id, AssignAircraftToFlightDto dto, CancellationToken ct)
    {
        var flight = await ctx.Flights
                              .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (flight is null)
        {
            _logger.LogWarning("Flight with ID {Id} not found", id);
            var error = Error.NotFound("Flight.NotFound", $"Flight with ID {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var aircraft = await ctx.Aircraft
                                .FirstOrDefaultAsync(a => a.Id == dto.AircraftId, ct);
        if (aircraft is null)
        {
            _logger.LogWarning("Aircraft with ID {Id} not found", dto.AircraftId);
            var error = Error.NotFound("Aircraft.NotFound", $"Aircraft with ID {dto.AircraftId} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        flight.AssignAircraft(aircraft);
        await ctx.SaveChangesAsync(ct);
        await _bus.Publish(new AircraftAssignedToFlightEvent(flight.Id, aircraft.Id), ct);
        return TypedResults.Ok(flight.ToDto());
    }
}
