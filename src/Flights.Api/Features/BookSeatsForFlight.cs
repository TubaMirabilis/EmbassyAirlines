using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Domain.Bookings;
using Flights.Api.Domain.Passengers;
using Flights.Api.Extensions;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;

namespace Flights.Api.Features;

public static class BookSeatsForFlight
{
    public sealed record Command(CreateBookingDto Dto) : ICommand<ErrorOr<Booking>>;
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Dto.FlightId)
                .NotEmpty()
                    .WithMessage("FlightId is required.");
            RuleFor(x => x.Dto.Seats)
                .NotEmpty()
                    .WithMessage("Please provide at least one seat for each booking.");
        }
    }
    public sealed class Handler : ICommandHandler<Command, ErrorOr<Booking>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IValidator<Command> _validator;
        public Handler(ApplicationDbContext ctx, IValidator<Command> validator)
        {
            _ctx = ctx;
            _validator = validator;
        }
        public async ValueTask<ErrorOr<Booking>> Handle(Command command, CancellationToken cancellationToken)
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
                return Error.NotFound("Query.NotFound", "Flight not found.");
            }
            var seatIds = command.Dto.Seats.Keys;
            var seats = flight.Seats.Where(s => seatIds.Contains(s.Id)).ToList();
            if (seats.Count != seatIds.Count)
            {
                return Error.Validation("Query.ValidationFailed", "One or more seats are invalid");
            }
            if (seats.Any(s => s.IsBooked))
            {
                return Error.Validation("Query.ValidationFailed", "One or more seats are already booked.");
            }
            var passengers = seatIds.Zip(command.Dto.Seats.Values, (seatId, passenger) => (seatId, passenger))
                                    .ToDictionary(x => x.seatId, x => Passenger.Create(x.passenger.FirstName, x.passenger.LastName));
            flight.BookSeats(passengers);
            var booking = Booking.Create(passengers.Select(p => p.Value), flight);
            return booking;
        }
    }
}
