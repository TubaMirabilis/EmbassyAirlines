using ErrorOr;
using Flights.Api.Database;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class CancelBooking
{
    public sealed record Command(Guid Id) : ICommand<ErrorOr<Unit>>;
    public sealed class Handler : ICommandHandler<Command, ErrorOr<Unit>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx) => _ctx = ctx;
        public async ValueTask<ErrorOr<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var booking = await _ctx.Bookings.Where(b => b.Id == command.Id).SingleOrDefaultAsync(cancellationToken);
            if (booking is null)
            {
                return Error.NotFound("Booking.NotFound", $"Booking with id {command.Id} was not found.");
            }
            booking.Cancel();
            await _ctx.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
public sealed class CancelBookingEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapDelete("itineraries/{itineraryId}/bookings/{bookingId}", CancelBooking)
              .WithName("cancelBooking")
              .WithOpenApi();
    private static async Task<IResult> CancelBooking([FromServices] ISender sender, [FromRoute] Guid bookingId, CancellationToken ct)
    {
        var command = new CancelBooking.Command(bookingId);
        var result = await sender.Send(command, ct);
        return result.Match(
            _ => Results.NoContent(),
            ErrorHandlingHelper.HandleProblems);
    }
}
