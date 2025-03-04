using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Domain.Journeys;
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

public static class SearchForJourneysByRouteAndDate
{
    public sealed record Query(string Departure, string Destination, string Date) : IQuery<ErrorOr<JourneyListDto>>;
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Departure)
                .Matches("^[A-Z]{3}$")
                .WithMessage("IATA Code must consist of 3 uppercase letters only.");
            RuleFor(x => x.Destination)
                .Matches("^[A-Z]{3}$")
                .WithMessage("IATA Code must consist of 3 uppercase letters only.");
            RuleFor(x => x.Date)
                .Custom((date, context) =>
                {
                    if (string.IsNullOrWhiteSpace(date))
                    {
                        context.AddFailure("Date is required.");
                        return;
                    }
                    var parseResult = LocalDatePattern.Iso
                                                      .Parse(date);
                    if (!parseResult.Success)
                    {
                        context.AddFailure("Invalid date format. Please use yyyy-MM-dd.");
                    }
                });
            RuleFor(x => x)
                .Must(x => x.Departure != x.Destination)
                .WithMessage("Destination cannot be the same as departure.");
        }
    }
    public sealed class Handler : IQueryHandler<Query, ErrorOr<JourneyListDto>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IJourneyService _js;
        private readonly ILogger<Handler> _logger;
        private readonly IValidator<Query> _validator;
        public Handler(ApplicationDbContext ctx, IJourneyService js, IValidator<Query> validator, ILogger<Handler> logger)
        {
            _ctx = ctx;
            _js = js;
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
                                              .Parse(query.Date);
            if (!parseResult.Success)
            {
                return Error.Validation("Query.ValidationFailed", "Invalid date format. Please use yyyy-MM-dd");
            }
            var localDate = parseResult.Value;
            var departureAirport = await _ctx.Airports
                                             .AsNoTracking()
                                             .SingleOrDefaultAsync(a => a.IataCode == query.Departure, cancellationToken);
            if (departureAirport is null)
            {
                return Error.NotFound("Airport.NotFound", $"Airport with IATA code {query.Departure} not found.");
            }
            var now = SystemClock.Instance.GetCurrentInstant();
            var nowLocal = now.InZone(DateTimeZoneProviders.Tzdb[departureAirport.TimeZoneId]).LocalDateTime;
            if (nowLocal.Date > localDate)
            {
                return Error.Validation("Query.ValidationFailed", "Departure date cannot be in the past.");
            }
            var flights = await _ctx.Flights
                                    .AsNoTracking()
                                    .Where(f => f.DepartureLocalTime.Date >= localDate &&
                                                f.DepartureLocalTime.Date <= localDate.PlusDays(7))
                                    .ToListAsync(cancellationToken);
            flights = flights.Where(f => f.DepartureInstant > now).ToList();
            var directFlights = flights.Where(f => f.DepartureAirport.IataCode == query.Departure &&
                                                   f.ArrivalAirport.IataCode == query.Destination &&
                                                   f.DepartureLocalTime.Date == localDate).ToList();
            if (directFlights.Count == 0)
            {
                var multiLegJourneys = _js.GetMultiLegJourneysOrderedByArrivalTime(flights, query.Departure, query.Destination, localDate, 3);
                var list = new List<List<FlightDto>>();
                foreach (var journey in multiLegJourneys)
                {
                    list.Add(journey.Select(f => f.ToDto()).ToList());
                }
                return new JourneyListDto(list);
            }
            var journeys = directFlights.Select(df => new FlightDto[] { df.ToDto() });
            return new JourneyListDto(journeys);
        }
    }
}
public sealed class SearchForJourneysByRouteAndDateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("journeys", SearchForJourneysByRouteAndDate)
              .WithName("searchForJourneysByRouteAndDate")
              .WithOpenApi();
    private static async Task<IResult> SearchForJourneysByRouteAndDate([FromServices] ISender sender,
        [FromQuery] string departure, string destination, string date, CancellationToken ct)
    {
        departure = departure.ToUpperInvariant();
        destination = destination.ToUpperInvariant();
        var query = new SearchForJourneysByRouteAndDate.Query(departure, destination, date);
        var result = await sender.Send(query, ct);
        return result.Match(
            Results.Ok,
            ErrorHandlingHelper.HandleProblems);
    }
}
