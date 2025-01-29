using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Domain.Bookings;
using Flights.Api.Domain.Passengers;
using Flights.Api.Extensions;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Sqids;

namespace Flights.Api.Features;

public static class BookSeatsForFlight
{
    public sealed record Command(CreateBookingDto Dto) : ICommand<ErrorOr<BookingDto>>;
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Dto.FlightId)
                .NotEmpty();
            RuleFor(x => x.Dto.Seats)
                .NotEmpty();
            RuleFor(x => x.Dto.LeadPassengerEmail)
                .MaximumLength(100);
        }
    }
    public sealed class Handler : ICommandHandler<Command, ErrorOr<BookingDto>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IValidator<Command> _validator;
        private readonly SqidsEncoder<int> _sqids;
        public Handler(ApplicationDbContext ctx, IValidator<Command> validator, SqidsEncoder<int> sqids)
        {
            _ctx = ctx;
            _validator = validator;
            _sqids = sqids;
        }
        public async ValueTask<ErrorOr<BookingDto>> Handle(Command command, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid(out var formattedErrors))
            {
                return Error.Validation("Query.ValidationFailed", formattedErrors);
            }
            var flight = await _ctx.Flights
                                   .Where(f => f.Id == command.Dto.FlightId)
                                   .SingleOrDefaultAsync(cancellationToken);
            if (flight is null)
            {
                return Error.NotFound("Query.NotFound", "Flight not found");
            }
            var seatIds = command.Dto.Seats.Keys;
            var seats = flight.Seats.Where(s => seatIds.Contains(s.Id)).ToList();
            if (seats.Count != seatIds.Count)
            {
                return Error.Validation("Query.ValidationFailed", "One or more seats are invalid");
            }
            if (seats.Any(s => s.IsBooked))
            {
                return Error.Validation("Query.ValidationFailed", "One or more seats are already booked");
            }
            var passengers = command.Dto.Seats.Select(s => Passenger.Create(s.Value.FirstName, s.Value.LastName)).ToList();
            var count = flight.Seats.Count(s => s.IsBooked);
            var reference = _sqids.Encode(count);
            var booking = Booking.Create(seats, passengers, reference, command.Dto.LeadPassengerEmail);
            _ctx.Bookings.Add(booking);
            await _ctx.SaveChangesAsync(cancellationToken);
            return booking.ToDto();
        }
    }
}
public sealed class BookSeatForFlightEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("bookings", BookSeats)
              .WithName("BookSeatsForFlight")
              .WithOpenApi();
    private static async Task<IResult> BookSeats([FromServices] ISender sender, [FromBody] CreateBookingDto dto, CancellationToken ct)
    {
        var command = new BookSeatsForFlight.Command(dto);
        var result = await sender.Send(command, ct);
        return result.Match(
            booking => Results.Created($"bookings/{booking.Reference}", booking),
            ErrorHandlingHelper.HandleProblems);
    }
}
