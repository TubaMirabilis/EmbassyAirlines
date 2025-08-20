using ErrorOr;
using Flights.Api.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Flights.Api.Endpoints;

internal sealed class GetFlightEndpoint : IEndpoint
{
    private readonly ApplicationDbContext _ctx;
    public GetFlightEndpoint(ApplicationDbContext ctx) => _ctx = ctx;
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights/{id}", InvokeAsync);
    private async Task<IResult> InvokeAsync(Guid id, CancellationToken ct)
    {
        var flight = await _ctx.Flights
                               .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (flight is null)
        {
            var error = Error.NotFound("Flight.NotFound", $"Flight with ID {id} not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        return TypedResults.Ok(flight.ToDto());
    }
}
