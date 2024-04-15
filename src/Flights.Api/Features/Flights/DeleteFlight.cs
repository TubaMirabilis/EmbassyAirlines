using Carter;
using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Entities;
using Flights.Api.Enums;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Flights.Api.Features.Flights;

public static class DeleteFlight
{
    public sealed record Command(Guid Id) : IRequest<ErrorOr<Unit>>;
    internal sealed class Handler : IRequestHandler<Command, ErrorOr<Unit>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ILogger<Handler> _logger;
        public Handler(ApplicationDbContext ctx, ILogger<Handler> logger)
        {
            _ctx = ctx;
            _logger = logger;
        }
        public async Task<ErrorOr<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting flight by id {id}", request.Id);
            var flight = await ValidateRequest(request, cancellationToken);
            if (flight.IsError)
            {
                return flight.FirstError;
            }
            _ctx.Flights.Remove(flight.Value);
            if (await _ctx.SaveChangesAsync(cancellationToken) == 0)
            {
                _logger.LogError("Failed to delete flight");
                return Error.Failure("Failed to delete flight");
            }
            _logger.LogInformation("Flight deleted");
            return Unit.Value;
        }
        private async Task<ErrorOr<Flight>> ValidateRequest(Command request, CancellationToken cancellationToken)
        {
            var flight = await _ctx.Flights
                            .Where(flight => flight.Id == request.Id)
                            .FirstOrDefaultAsync(cancellationToken);
            if (flight is null)
            {
                _logger.LogWarning("Flight not found");
                return Error.NotFound("Flight not found");
            }
            if (flight.Status is FlightStatus.EnRoute or FlightStatus.Diverting)
            {
                _logger.LogWarning("Cannot delete flight that is en route or diverting");
                return Error.Validation("Cannot delete flight that is en route or diverting");
            }
            if (flight.Status == FlightStatus.Arrived && (DateTime.UtcNow - flight.ArrivalTimeUtc).TotalDays <= 120)
            {
                _logger.LogWarning("Cannot delete flight that has arrived within the last 120 days");
                return Error.Validation("Cannot delete flight that has arrived within the last 120 days");
            }
            if (flight.Status == FlightStatus.Arrived && (DateTime.UtcNow - flight.ArrivalTimeUtc).TotalDays > 120)
            {
                return flight;
            }
            if (flight.TotalPassengers > 0 || flight.CheckedBags > 0)
            {
                _logger.LogWarning("Cannot delete flight with passengers or checked bags");
                return Error.Validation("Cannot delete flight with passengers or checked bags");
            }
            return flight;
        }
    }
}
public class DeleteFlightEndpoint : ICarterModule
{
    private readonly ILogger<DeleteFlightEndpoint> _logger;
    public DeleteFlightEndpoint(ILogger<DeleteFlightEndpoint> logger)
    {
        _logger = logger;
    }
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/flights/{id}", async (Guid id, ISender sender,
            IOutputCacheStore cache, CancellationToken ct) =>
        {
            _logger.LogInformation("Received request to delete flight by id {id}", id);
            var command = new DeleteFlight.Command(id);
            var result = await sender.Send(command, ct);
            if (!result.IsError)
            {
                await cache.EvictByTagAsync("flights", ct);
            }
            return result.Match(
                _ => Results.NoContent(),
                errors => ErrorHandlingHelper.HandleProblems(errors));
        }).WithName("Delete Flight")
        .WithOpenApi();
    }
}
