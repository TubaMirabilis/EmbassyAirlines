using ErrorOr;
using Flights.Api.Database;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Shared.Extensions;

namespace Flights.Api.Endpoints;

internal sealed class ScheduleFlightEndpoint : IEndpoint
{
    private readonly IBus _bus;
    private readonly ApplicationDbContext _ctx;
    private readonly IValidator<CreateOrUpdateFlightDto> _validator;
    public ScheduleFlightEndpoint(IBus bus, ApplicationDbContext ctx, IValidator<CreateOrUpdateFlightDto> validator)
    {
        _bus = bus;
        _ctx = ctx;
        _validator = validator;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("flights", InvokeAsync);
    private async Task<IResult> InvokeAsync(CreateOrUpdateFlightDto dto, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            var error = Error.Validation("Flight.Validation", formattedErrors);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var departureAirport = await _ctx.Airports
                                         .Where(a => a.Id == dto.DepartureAirportId)
                                         .SingleOrDefaultAsync(ct);
        if (departureAirport is null)
        {
            var error = Error.Validation("Flight.DepartureAirportNotFound", "Departure airport not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var arrivalAirport = await _ctx.Airports
                                        .Where(a => a.Id == dto.ArrivalAirportId)
                                        .SingleOrDefaultAsync(ct);
        if (arrivalAirport is null)
        {
            var error = Error.Validation("Flight.ArrivalAirportNotFound", "Arrival airport not found");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var departureTime = LocalDateTime.FromDateTime(dto.DepartureLocalTime);
        var departureInstant = departureTime.InZoneStrictly(departureAirport.TimeZone).ToInstant();
        if (departureInstant < SystemClock.Instance.GetCurrentInstant())
        {
            var error = Error.Validation("Flight.DepartureTimeInPast", "Departure time cannot be in the past");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var flight = Flight.Create(Guid.NewGuid());
        await _bus.Publish(new FlightScheduledEvent(), ct);
        return TypedResults.Created($"/flights/{flight.Id}", new FlightDto(flight.Id));
    }
}
