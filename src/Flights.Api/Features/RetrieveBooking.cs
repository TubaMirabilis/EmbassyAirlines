using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Extensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class RetrieveBooking
{
    public sealed record Query(Guid Id) : IQuery<ErrorOr<BookingDto>>;
    public sealed class Handler : IQueryHandler<Query, ErrorOr<BookingDto>>
    {
        private readonly ApplicationDbContext _ctx;
        public Handler(ApplicationDbContext ctx) => _ctx = ctx;
        public async ValueTask<ErrorOr<BookingDto>> Handle(Query query, CancellationToken cancellationToken)
        {
            var booking = await _ctx.Bookings
                                    .SingleOrDefaultAsync(b => b.Id == query.Id, cancellationToken);
            return booking is null
                ? Error.NotFound("Booking.NotFound", $"Booking with id {query.Id} was not found")
                : booking.ToDto();
        }
    }
    public sealed class RetrieveBookingEndpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
            => app.MapGet("bookings/{slug}", RetrieveBooking)
                  .WithName("RetrieveBooking")
                  .WithOpenApi();
        private static async Task<IResult> RetrieveBooking([FromServices] ISender sender, [FromRoute] Guid slug, CancellationToken ct)
        {
            var query = new Query(slug);
            var result = await sender.Send(query, ct);
            return result.Match(
                Results.Ok,
                ErrorHandlingHelper.HandleProblems);
        }
    }
}
