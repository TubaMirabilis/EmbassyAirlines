using ErrorOr;
using Flights.Api.Database;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class RemovePassengerFromBooking
{
    public sealed record Command(Guid BookingId, Guid PassengerId) : ICommand<ErrorOr<Unit>>;
    public sealed class Handler : ICommandHandler<Command, ErrorOr<Unit>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx) => _ctx = ctx;
        public async ValueTask<ErrorOr<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var booking = await _ctx.Bookings.Where(b => b.Id == command.BookingId).SingleOrDefaultAsync(cancellationToken);
            if (booking is null)
            {
                return Error.NotFound("Booking.NotFound", $"Booking with id {command.BookingId} not found");
            }
            booking.RemovePassenger(command.PassengerId);
            await _ctx.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
public sealed class RemovePassengerFromBookingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapDelete("itineraries/{itineraryId}/bookings/{bookingId}/passengers/{passengerId}", RemovePassengerFromBooking)
              .WithName("removePassengerFromBooking")
              .WithOpenApi();
    private static async Task<IResult> RemovePassengerFromBooking([FromServices] ISender sender, [FromRoute] Guid bookingId, [FromRoute] Guid passengerId, CancellationToken ct)
    {
        var command = new RemovePassengerFromBooking.Command(bookingId, passengerId);
        var result = await sender.Send(command, ct);
        return result.Match(
            _ => Results.NoContent(),
            ErrorHandlingHelper.HandleProblems);
    }
}
