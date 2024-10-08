using System.Globalization;
using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Entities;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class SearchForFlightsByRouteAndDate
{
    public sealed record Query(string? Departure, string? Destination, DateOnly Date) : IQuery<ErrorOr<IEnumerable<FlightDto>>>;
    public sealed class Handler : IQueryHandler<Query, ErrorOr<IEnumerable<FlightDto>>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx) => _ctx = ctx;
        public async ValueTask<ErrorOr<IEnumerable<FlightDto>>> Handle(
            Query query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query.Departure))
            {
                return Error.Validation("Flight.Departure", "Departure is required");
            }
            if (string.IsNullOrWhiteSpace(query.Destination))
            {
                return Error.Validation("Flight.Destination", "Destination is required");
            }
            if (query.Departure == query.Destination)
            {
                return Error.Validation("Flight.Destination", "Destination cannot be the same as departure");
            }
            var flights = await _ctx.Flights
                                    .Where(f =>
                                        f.Schedule.Departure == query.Departure &&
                                        f.Schedule.Destination == query.Destination &&
                                        f.Schedule.DepartureTime.Date == query.Date.ToDateTime(new TimeOnly(0)))
                                    .AsSplitQuery()
                                    .ToListAsync(cancellationToken);
            if (flights.TrueForAll(f => f.Status >= FlightStatus.CheckInClosed))
            {
                return Error.Validation("Flight.NoAvailability", "No flights available for the selected route and date");
            }
            return flights.Select(f => new FlightDto(f.Id, f.CreatedAt, f.UpdatedAt, f.FlightNumber,
                                       f.Schedule.Departure, f.Schedule.Destination, f.Schedule.DepartureTime,
                                       f.Schedule.ArrivalTime, f.Pricing.EconomyPrice, f.Pricing.BusinessPrice,
                                       f.AvailableSeats.Economy, f.AvailableSeats.Business))
                          .OrderBy(f => f.DepartureTime)
                          .ToList();
        }
    }
}
public sealed class SearchForFlightsByRouteAndDateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights", SearchForFlightsByRouteAndDate)
              .WithName("Get flights")
              .WithOpenApi();
    private static async Task<IResult> SearchForFlightsByRouteAndDate([FromServices] ISender sender,
        [FromQuery] string departure, string arrival, string date, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, CultureInfo.InvariantCulture, out var dateOnly))
        {
            return Results.BadRequest("Invalid date format. Please use yyyy-MM-dd");
        }
        var query = new SearchForFlightsByRouteAndDate.Query(departure?.ToUpperInvariant(),
            arrival?.ToUpperInvariant(), dateOnly);
        var result = await sender.Send(query, ct);
        return result.Match(
            flights => Results.Ok(flights),
            errors => ErrorHandlingHelper.HandleProblems(errors));
    }
}
