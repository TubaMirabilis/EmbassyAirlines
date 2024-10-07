using System.Globalization;
using Flights.Api.Database;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class SearchForFlightsByRouteAndDate
{
    public sealed record Query(string Departure, string Destination, DateOnly Date) : IQuery<IEnumerable<FlightDto>>;
    public sealed class Handler : IQueryHandler<Query, IEnumerable<FlightDto>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx) => _ctx = ctx;
        public async ValueTask<IEnumerable<FlightDto>> Handle(Query query, CancellationToken cancellationToken)
        {
            var flights = await _ctx.Flights
                                    .Where(f =>
                                        f.Schedule.Departure == query.Departure &&
                                        f.Schedule.Destination == query.Destination &&
                                        f.Schedule.DepartureTime == query.Date.ToDateTime(new TimeOnly(0)))
                                    .AsSplitQuery()
                                    .ToListAsync(cancellationToken);
            return flights.Select(f => new FlightDto(f.Id, f.CreatedAt, f.UpdatedAt, f.FlightNumber,
                f.Schedule.Departure, f.Schedule.Destination, f.Schedule.DepartureTime, f.Schedule.ArrivalTime,
                f.Pricing.EconomyPrice, f.Pricing.BusinessPrice, f.AvailableSeats.Economy, f.AvailableSeats.Business));
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
        if (DateOnly.TryParse(date, CultureInfo.InvariantCulture, out var dateOnly))
        {
            return Results.BadRequest("Invalid date format. Please use yyyy-MM-dd");
        }
        return Results.Ok(await sender.Send(
            new SearchForFlightsByRouteAndDate.Query(departure, arrival, dateOnly), ct));
    }
}
