using System.Globalization;
using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Entities;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class SearchForFlightsByRouteAndDate
{
    public sealed record Query(string? Departure, string? Destination, LocalDate Date)
        : IQuery<ErrorOr<IEnumerable<FlightDto>>>;
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
                                        f.Schedule.DepartureTime.Date == query.Date)
                                    .AsSplitQuery()
                                    .ToListAsync(cancellationToken);
            if (flights.Count != 0 && flights.TrueForAll(f => f.Schedule.DepartureTime.ToDateTimeOffset() < DateTimeOffset.Now))
            {
                return Error.Validation("Flight.DepartureTime", "All flights have already departed");
            }
            return flights.Select(f => new FlightDto(f.Id, f.CreatedAt.ToDateTimeOffset(), f.UpdatedAt.ToDateTimeOffset(), f.FlightNumber, f.Schedule.Departure, f.Schedule.Destination, f.Schedule.DepartureTime.ToDateTimeOffset(), f.Schedule.ArrivalTime.ToDateTimeOffset(), f.Pricing.EconomyPrice, f.Pricing.BusinessPrice, f.AvailableSeats.Economy, f.AvailableSeats.Business))
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
        if (!LocalDatePattern.Iso.Parse(date).TryGetValue(LocalDate.MinIsoValue, out var localDate))
        {
            return Results.BadRequest("Invalid date format. Please use yyyy-MM-dd");
        }
        var query = new SearchForFlightsByRouteAndDate.Query(departure?.ToUpperInvariant(),
            arrival?.ToUpperInvariant(), localDate);
        var result = await sender.Send(query, ct);
        return result.Match(
            flights => Results.Ok(flights),
            errors => ErrorHandlingHelper.HandleProblems(errors));
    }
}
