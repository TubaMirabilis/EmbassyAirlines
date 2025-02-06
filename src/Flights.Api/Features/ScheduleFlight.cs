using ErrorOr;
using Flights.Api.Database;
using Flights.Api.Domain.Flights;
using Flights.Api.Domain.Seats;
using Flights.Api.Extensions;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;

namespace Flights.Api.Features;

public static class ScheduleFlight
{
    public sealed record Command(ScheduleFlightDto Dto) : ICommand<ErrorOr<FlightDto>>;
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Dto.FlightNumber)
                .MaximumLength(6)
                    .WithMessage("Flight number must be 6 characters or less.")
                .Matches("^[A-Z0-9]+$")
                    .WithMessage("Flight number must be alphanumeric.");
            RuleFor(x => x.Dto.DepartureAirportId)
                .NotEmpty()
                    .WithMessage("Departure airport id is required.");
            RuleFor(x => x.Dto.ArrivalAirportId)
                .NotEmpty()
                    .WithMessage("Arrival airport id is required.");
            RuleFor(x => x.Dto.DepartureLocalTime)
                .NotEmpty()
                    .WithMessage("Departure time is required.");
            RuleFor(x => x.Dto.ArrivalLocalTime)
                .NotEmpty()
                    .WithMessage("Arrival time is required.");
            RuleFor(x => x.Dto.EconomyPrice)
                .GreaterThan(0)
                    .WithMessage("Economy price must be greater than 0.");
            RuleFor(x => x.Dto.BusinessPrice)
                .GreaterThan(0)
                    .WithMessage("Business price must be greater than 0.");
            RuleFor(x => x.Dto.EquipmentType)
                .NotEmpty()
                    .WithMessage("Equipment type is required.")
                .MaximumLength(4)
                    .WithMessage("Equipment type must be 4 characters or less.");
        }
    }
    public sealed class Handler : ICommandHandler<Command, ErrorOr<FlightDto>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ILogger<Handler> _logger;
        private readonly ISeatService _seatService;
        private readonly IValidator<Command> _validator;
        public Handler(ApplicationDbContext ctx, ILogger<Handler> logger, ISeatService seatService, IValidator<Command> validator)
        {
            _ctx = ctx;
            _logger = logger;
            _seatService = seatService;
            _validator = validator;
        }
        public async ValueTask<ErrorOr<FlightDto>> Handle(Command command, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(command, cancellationToken);
            if (!validationResult.IsValid(out var formattedErrors))
            {
                _logger.LogWarning("Validation failed. Errors: {Errors}", formattedErrors);
                return Error.Validation("Command.ValidationFailed", formattedErrors);
            }
            var departureAirport = await _ctx.Airports
                                             .Where(a => a.Id == command.Dto.DepartureAirportId)
                                             .SingleOrDefaultAsync(cancellationToken);
            if (departureAirport == null)
            {
                _logger.LogWarning("Departure airport with id {Id} not found.", command.Dto.DepartureAirportId);
                return Error.NotFound("DepartureAirport.NotFound", $"Departure airport with id {command.Dto.DepartureAirportId} not found.");
            }
            var arrivalAirport = await _ctx.Airports
                                           .Where(a => a.Id == command.Dto.ArrivalAirportId)
                                           .SingleOrDefaultAsync(cancellationToken);
            if (arrivalAirport == null)
            {
                _logger.LogWarning("Arrival airport with id {Id} not found.", command.Dto.ArrivalAirportId);
                return Error.NotFound("ArrivalAirport.NotFound", $"Arrival airport with id {command.Dto.ArrivalAirportId} not found.");
            }
            var departureTime = LocalDateTime.FromDateTime(command.Dto.DepartureLocalTime);
            var departureInstant = departureTime.InZoneStrictly(departureAirport.TimeZone).ToInstant();
            if (departureInstant < SystemClock.Instance.GetCurrentInstant())
            {
                _logger.LogWarning("Departure time {DepartureTime} is in the past.", command.Dto.DepartureLocalTime);
                return Error.Validation("DepartureTime.Invalid", "Departure time is in the past.");
            }
            var arrivalTime = LocalDateTime.FromDateTime(command.Dto.ArrivalLocalTime);
            var arrivalInstant = arrivalTime.InZoneStrictly(arrivalAirport.TimeZone).ToInstant();
            if (arrivalInstant < departureInstant)
            {
                _logger.LogWarning("Arrival time {ArrivalTime} is before departure time {DepartureTime}.", command.Dto.ArrivalLocalTime, command.Dto.DepartureLocalTime);
                return Error.Validation("ArrivalTime.Invalid", "Arrival time is before departure time.");
            }
            var seats = _seatService.CreateSeats(command.Dto.EquipmentType, command.Dto.EconomyPrice, command.Dto.BusinessPrice);
            var args = new FlightCreationArgs
            {
                ArrivalAirport = arrivalAirport,
                ArrivalLocalTime = arrivalTime,
                DepartureAirport = departureAirport,
                DepartureLocalTime = departureTime,
                FlightNumber = command.Dto.FlightNumber,
                Seats = seats
            };
            var flight = Flight.Create(args);
            _ctx.Flights
                .Add(flight);
            await _ctx.SaveChangesAsync(cancellationToken);
            return flight.ToDto();
        }
    }
}
public sealed class ScheduleFlightEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("flights", ScheduleFlight)
              .WithName("scheduleFlight")
              .Produces(StatusCodes.Status201Created)
              .WithOpenApi();
    private static async Task<IResult> ScheduleFlight([FromServices] ISender sender, [FromBody] ScheduleFlightDto dto, CancellationToken ct)
    {
        var command = new ScheduleFlight.Command(dto);
        var result = await sender.Send(command, ct);
        return result.Match(
            res => Results.Created($"flights/{res.Id}", res),
            ErrorHandlingHelper.HandleProblems);
    }
}
