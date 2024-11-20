using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Domain.Flights;
using Flights.Api.Extensions;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime.Text;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class SearchForFlightsByRouteAndDate
{
    public sealed record Query(string? Departure, string? Destination, string? Date)
        : IQuery<ErrorOr<IEnumerable<FlightDto>>>;
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
    public sealed class Handler : IQueryHandler<Query, ErrorOr<IEnumerable<FlightDto>>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IValidator<Query> _validator;
        public Handler(ApplicationDbContext ctx, IValidator<Query> validator)
        {
            _ctx = ctx;
            _validator = validator;
        }
        public async ValueTask<ErrorOr<IEnumerable<FlightDto>>> Handle(
            Query query, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid(out var formattedErrors))
            {
                return Error.Validation("Query.ValidationFailed", formattedErrors);
            }
            var parseResult = LocalDatePattern.Iso
                                              .Parse(query.Date!);
            if (!parseResult.Success)
            {
                return Error.Validation("Query.ValidationFailed", "Invalid date format. Please use yyyy-MM-dd");
            }
            var localDate = parseResult.Value;
            var flights = await _ctx.Flights
                                    .Where(f =>
                                        f.Schedule.DepartureAirport.IataCode == query.Departure &&
                                        f.Schedule.DestinationAirport.IataCode == query.Destination &&
                                        f.Schedule.DepartureTime.Date == localDate)
                                    .AsSplitQuery()
                                    .ToListAsync(cancellationToken);
            if (flights.Count != 0 && AllFlightsDeparted(flights))
            {
                return Error.Validation("Query.NoMoreFlights", "All flights have already departed");
            }
            return GetFlights(flights);
        }
        private static List<FlightDto> GetFlights(IEnumerable<Flight> flights)
            =>
            [..
                flights.Select(f => f.ToDto())
                       .OrderBy(f => f.DepartureTime)
            ];
        private static bool AllFlightsDeparted(List<Flight> flights)
            => flights.TrueForAll(f => f.Schedule
                                        .DepartureTime
                                        .ToDateTimeOffset() < DateTimeOffset.Now);
    }
}
public sealed class SearchForFlightsByRouteAndDateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights", SearchForFlightsByRouteAndDate)
              .WithName("Get flights")
              .WithOpenApi();
    private static async Task<IResult> SearchForFlightsByRouteAndDate([FromServices] ISender sender,
        [FromQuery] string? departure, string? destination, string? date, CancellationToken ct)
    {
        departure = departure?.ToUpperInvariant();
        destination = destination?.ToUpperInvariant();
        var query = new SearchForFlightsByRouteAndDate.Query(departure, destination, date);
        var result = await sender.Send(query, ct);
        return result.Match(
            flights => Results.Ok(flights),
            errors => ErrorHandlingHelper.HandleProblems(errors));
    }
}
