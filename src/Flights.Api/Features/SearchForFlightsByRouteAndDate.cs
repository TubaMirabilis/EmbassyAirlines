using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Domain.Flights;
using Flights.Api.Extensions;
using FluentValidation;
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
    public sealed record Query(string Departure, string Destination, string Date) : IQuery<ErrorOr<JourneyListDto>>;
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Departure)
                .NotEmpty()
                .WithMessage("Departure is required");
            RuleFor(x => x.Destination)
                .NotEmpty()
                .WithMessage("Destination is required");
            RuleFor(x => x.Date)
                .Custom((date, context) =>
                {
                    if (string.IsNullOrWhiteSpace(date))
                    {
                        context.AddFailure("Date is required");
                        return;
                    }
                    var parseResult = LocalDatePattern.Iso
                                                      .Parse(date);
                    if (!parseResult.Success)
                    {
                        context.AddFailure("Invalid date format. Please use yyyy-MM-dd");
                    }
                });
            RuleFor(x => x)
                .Must(x => x.Departure != x.Destination)
                .WithMessage("Destination cannot be the same as departure");
        }
    }
    public sealed class Handler : IQueryHandler<Query, ErrorOr<JourneyListDto>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ILogger<Handler> _logger;
        private readonly IValidator<Query> _validator;
        public Handler(ApplicationDbContext ctx, IValidator<Query> validator, ILogger<Handler> logger)
        {
            _ctx = ctx;
            _logger = logger;
            _validator = validator;
        }
        public async ValueTask<ErrorOr<JourneyListDto>> Handle(Query query, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid(out var formattedErrors))
            {
                _logger.LogWarning("Validation failed for query: {Query}. Errors: {Errors}", query, formattedErrors);
                return Error.Validation("Query.ValidationFailed", formattedErrors);
            }
            var parseResult = LocalDatePattern.Iso
                                              .Parse(query.Date!);
            if (!parseResult.Success)
            {
                return Error.Validation("Query.ValidationFailed", "Invalid date format. Please use yyyy-MM-dd");
            }
            var localDate = parseResult.Value;
            var flights = await _ctx.Flights.Where(f => f.ScheduledDeparture.Date >= localDate && f.ScheduledDeparture.Date <= localDate.PlusDays(7))
                                            .Include(f => f.DepartureAirport)
                                            .Include(f => f.ArrivalAirport)
                                            .ToListAsync(cancellationToken);
            var directFlights = flights.Where(f => f.DepartureAirport.IataCode == query.Departure && f.ArrivalAirport.IataCode == query.Destination && f.ScheduledDeparture.Date == localDate).ToList();
            if (directFlights.Count == 0)
            {
                return GetThreeFastestMultiLegItineraries(flights, query.Departure, query.Destination, localDate);
            }
            var journeys = directFlights.Select(df => new FlightDto[] { df.ToDto() });
            return new JourneyListDto(journeys);
        }
        private static JourneyListDto GetThreeFastestMultiLegItineraries(List<Flight> flights, string departure, string destination, LocalDate localDate)
        {
            var flightsByDeparture = flights.GroupBy(f => f.DepartureAirport.IataCode)
                                            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.DepartureInstant).ToList());
            if (!flightsByDeparture.TryGetValue(departure, out var initialFlights))
            {
                return new JourneyListDto(Array.Empty<FlightDto[]>());
            }
            var allFlights = initialFlights.Where(f => f.ScheduledDeparture.Date == localDate)
    .ToList();
            var journeys = new List<FlightDto[]>();
            foreach (var flight in allFlights)
            {
                if (flightsByDeparture.TryGetValue(flight.ArrivalAirport.IataCode, out var nextFlightCandidates))
                {
                    var nextFlights = nextFlightCandidates.Where(f => f.DepartureInstant > flight.ArrivalInstant.Plus(Duration.FromMinutes(30)))
                                                          .ToList();
                    foreach (var nextFlight in nextFlights)
                    {
                        if (nextFlight.ArrivalAirport.IataCode == destination)
                        {
                            journeys.Add(new[] { flight.ToDto(), nextFlight.ToDto() });
                        }
                        else
                        {
                            if (flightsByDeparture.TryGetValue(nextFlight.ArrivalAirport.IataCode, out var finalFlightCandidates))
                            {
                                var finalFlights = finalFlightCandidates.Where(f => f.DepartureInstant > nextFlight.ArrivalInstant.Plus(Duration.FromMinutes(30)))
                                                                        .ToList();
                                foreach (var finalFlight in finalFlights)
                                {
                                    if (finalFlight.ArrivalAirport.IataCode == destination)
                                    {
                                        journeys.Add(new[] { flight.ToDto(), nextFlight.ToDto(), finalFlight.ToDto() });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            var sortedJourneys = journeys.OrderBy(j => j[^1].ArrivalTime).Take(3).ToList();
            return new JourneyListDto(sortedJourneys);
        }
    }
}
public sealed class SearchForFlightsByRouteAndDateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("journeys", SearchForFlightsByRouteAndDate)
              .WithName("searchForFlightsByRouteAndDate")
              .WithOpenApi();
    private static async Task<IResult> SearchForFlightsByRouteAndDate([FromServices] ISender sender,
        [FromQuery] string departure, string destination, string date, CancellationToken ct)
    {
        departure = departure.ToUpperInvariant();
        destination = destination.ToUpperInvariant();
        var query = new SearchForFlightsByRouteAndDate.Query(departure, destination, date);
        var result = await sender.Send(query, ct);
        return result.Match(
            Results.Ok,
            ErrorHandlingHelper.HandleProblems);
    }
}
