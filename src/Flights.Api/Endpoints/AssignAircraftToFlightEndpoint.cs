using ErrorOr;
using Flights.Api.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class AssignAircraftToFlightEndpoint : IEndpoint
{
    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<AssignAircraftToFlightEndpoint> _logger;
    public AssignAircraftToFlightEndpoint(IServiceScopeFactory factory, ILogger<AssignAircraftToFlightEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPatch("flights/{id}/aircraft", InvokeAsync);
    private async Task<IResult> InvokeAsync(Guid id, AssignAircraftToFlightDto dto, CancellationToken ct)
    {
        using var scope = _factory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
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
        return TypedResults.Ok(flight.ToDto());
    }
}
