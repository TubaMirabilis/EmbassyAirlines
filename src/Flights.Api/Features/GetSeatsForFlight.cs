using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Domain.Seats;
using Flights.Api.Extensions;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class GetSeatsForFlight
{
    public sealed record Query(Guid FlightId, string? SeatType) : IQuery<ErrorOr<IEnumerable<SeatDto>>>;
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.FlightId)
                .NotEmpty()
                .WithMessage("FlightId is required.");
        }
    }
    public sealed class Handler : IQueryHandler<Query, ErrorOr<IEnumerable<SeatDto>>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IValidator<Query> _validator;
        public Handler(ApplicationDbContext ctx, IValidator<Query> validator)
        {
            _ctx = ctx;
            _validator = validator;
        }
        public async ValueTask<ErrorOr<IEnumerable<SeatDto>>> Handle(Query query, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid(out var formattedErrors))
            {
                return Error.Validation("Query.ValidationFailed", formattedErrors);
            }
            var flight = await _ctx.Flights
                                  .AsNoTracking()
                                  .SingleOrDefaultAsync(f => f.Id == query.FlightId, cancellationToken);
            if (flight is null)
            {
                return Error.NotFound("Flight.NotFound", $"Flight with id {query.FlightId} was not found.");
            }
            var seats = flight.Seats
                              .Where(s => s.FlightId == query.FlightId);
            if (Enum.TryParse<SeatType>(query.SeatType, true, out var seatType))
            {
                seats = seats.Where(s => s.SeatType == seatType);
            }
            var seatsList = seats.ToList();
            return seatsList.Select(s => s.ToDto())
                            .OrderBy(s => s.SeatNumber)
                            .ToList();
        }
    }
}
public sealed class GetSeatsForFlightEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("flights/{flightId}/seats", GetSeats)
              .WithName("getSeatsForFlight")
              .WithOpenApi();
    private static async Task<IResult> GetSeats([FromServices] ISender sender,
        [FromRoute] Guid flightId, [FromQuery] string? seatType, CancellationToken ct)
    {
        var query = new GetSeatsForFlight.Query(flightId, seatType);
        var result = await sender.Send(query, ct);
        return result.Match(
            Results.Ok,
            ErrorHandlingHelper.HandleProblems);
    }
}
