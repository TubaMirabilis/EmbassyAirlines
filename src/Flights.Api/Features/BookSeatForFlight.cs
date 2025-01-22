using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Domain.Bookings;
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

public static class BookSeatForFlight
{
    public sealed record Command(BookSeatRequest Request) : ICommand<ErrorOr<BookingDto>>;
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Request.SeatId).NotEmpty();
            RuleFor(x => x.Request.PassengerName)
                .NotEmpty()
                .MaximumLength(100);
            RuleFor(x => x.Request.PassengerEmail)
                .EmailAddress()
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
            var seat = await _ctx.Seats
                                 .SingleOrDefaultAsync(s => s.Id == command.Request.SeatId, cancellationToken);
            if (seat is null)
            {
                return Error.NotFound("Seat.NotFound", $"Seat with id {command.Request.SeatId} not found");
            }
            if (seat.IsBooked)
            {
                return Error.Conflict("Seat.AlreadyBooked", $"Seat with id {command.Request.SeatId} is already booked");
            }
            seat.MarkAsBooked();
            var count = await _ctx.Bookings
                                  .AsNoTracking()
                                  .Include(b => b.Seat)
                                  .CountAsync(b => b.Seat.FlightId == seat.FlightId, cancellationToken);
            var reference = _sqids.Encode(count);
            var booking = Booking.Create(seat, reference, command.Request.PassengerName, command.Request.PassengerEmail);
            _ctx.Bookings.Add(booking);
            await _ctx.SaveChangesAsync(cancellationToken);
            return booking.ToDto();
        }
    }
}
public sealed class BookSeatForFlightEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("bookings", BookSeat)
              .WithName("BookSeatForFlight")
              .WithOpenApi();
    private static async Task<IResult> BookSeat([FromServices] ISender sender, [FromBody] BookSeatRequest request, CancellationToken ct)
    {
        var command = new BookSeatForFlight.Command(request);
        var result = await sender.Send(command, ct);
        return result.Match(
            booking => Results.Created($"bookings/{booking.Reference}", booking),
            ErrorHandlingHelper.HandleProblems);
    }
}
