using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Entities;
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
                .WithMessage("FlightId is required");
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
            var seats = _ctx.Seats
                            .Where(s => s.FlightId == query.FlightId);
            if (Enum.TryParse<SeatType>(query.SeatType, true, out var seatType))
            {
                seats = seats.Where(s => s.SeatType == seatType);
            }
            var seatsList = await seats.AsSplitQuery()
                                       .ToListAsync(cancellationToken);
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
              .WithName("GetSeats")
              .WithOpenApi();
    private static async Task<IResult> GetSeats([FromServices] ISender sender,
        [FromRoute] Guid flightId, [FromQuery] string? seatType, CancellationToken ct)
    {
        var query = new GetSeatsForFlight.Query(flightId, seatType);
        var result = await sender.Send(query, ct);
        return result.Match(
            seats => Results.Ok(seats),
            errors => ErrorHandlingHelper.HandleProblems(errors));
    }
}
