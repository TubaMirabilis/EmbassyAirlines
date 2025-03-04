using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Domain.Bookings;
using Flights.Api.Domain.Itineraries;
using Flights.Api.Extensions;
using MassTransit;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Sqids;

namespace Flights.Api.Features;

public static class CreateItinerary
{
    public sealed record Command(CreateItineraryDto Dto) : ICommand<ErrorOr<ItineraryDto>>;
    public sealed class Handler : ICommandHandler<Command, ErrorOr<ItineraryDto>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly IBus _bus;
        private readonly ISender _sender;
        private readonly SqidsEncoder<int> _sqids;
        public Handler(ApplicationDbContext ctx, IBus bus, ISender sender, SqidsEncoder<int> sqids)
        {
            _ctx = ctx;
            _bus = bus;
            _sender = sender;
            _sqids = sqids;
        }
        public async ValueTask<ErrorOr<ItineraryDto>> Handle(Command command, CancellationToken cancellationToken)
        {
            if (!command.Dto.Bookings.Any())
            {
                return Error.Validation("Itinerary.BookingsEmpty", "Please provide at least one booking request.");
            }
            List<Booking> bookings = [];
            foreach (var bookingDto in command.Dto.Bookings)
            {
                var bookingCommand = new BookSeatsForFlight.Command(bookingDto);
                var booking = await _sender.Send(bookingCommand, cancellationToken);
                if (booking.IsError)
                {
                    return booking.FirstError;
                }
                bookings.Add(booking.Value);
            }
            var count = await _ctx.Itineraries.CountAsync(cancellationToken);
            var reference = _sqids.Encode(count);
            var itinerary = Itinerary.Create(bookings, reference, command.Dto.LeadPassengerEmail);
            _ctx.Itineraries
                .Add(itinerary);
            await _ctx.SaveChangesAsync(cancellationToken);
            var dto = itinerary.ToDto();
            if (!string.IsNullOrWhiteSpace(dto.LeadPassengerEmail))
            {
                await _bus.Publish(new ItineraryCreatedEvent(dto.Bookings, dto.Reference, dto.LeadPassengerEmail, dto.TotalPrice), cancellationToken);
            }
            return dto;
        }
    }
}
public sealed class CreateItineraryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("itineraries", CreateItinerary)
              .WithName("createItinerary")
              .Produces(StatusCodes.Status201Created)
              .WithOpenApi();
    private static async Task<IResult> CreateItinerary([FromServices] ISender sender, [FromBody] CreateItineraryDto dto, CancellationToken ct)
    {
        var command = new CreateItinerary.Command(dto);
        var result = await sender.Send(command, ct);
        return result.Match(
            res => Results.Created($"itineraries/{res.Reference}", res),
            ErrorHandlingHelper.HandleProblems);
    }
}
