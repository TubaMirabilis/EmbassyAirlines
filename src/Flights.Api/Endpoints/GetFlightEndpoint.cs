using ErrorOr;
using Flights.Api.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class GetFlightEndpoint : IEndpoint
{
    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<GetFlightEndpoint> _logger;
    public GetFlightEndpoint(IServiceScopeFactory factory, ILogger<GetFlightEndpoint> logger)
    {
        _factory = factory;
        _logger = logger;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights/{id}", InvokeAsync);
    private async Task<IResult> InvokeAsync(Guid id, CancellationToken ct)
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
        return TypedResults.Ok(flight.ToDto());
    }
}
