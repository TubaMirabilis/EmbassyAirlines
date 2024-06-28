using Carter;
using ErrorOr;
using Flights.Api.Contracts;
using Flights.Api.Database;
using Flights.Api.Domain.Flights;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Flights.Api.Features.Flights;

public static class AddFlight
{
    public sealed record Command(AddFlightRequest Request) : IRequest<ErrorOr<FlightResponse>>;
    public sealed class AddFlightRequestValidator : AbstractValidator<AddFlightRequest>
    {
        public AddFlightRequestValidator()
        {
            RuleFor(x => x.Number).NotEmpty().MaximumLength(10);
            RuleFor(x => x.NumberIataFormat).NotEmpty().MaximumLength(10);
            RuleFor(x => x.NumberIcaoFormat).NotEmpty().MaximumLength(10);
            RuleFor(x => x.DepartureTimeUtc).NotEmpty().LessThan(x => x.ArrivalTimeUtc);
            RuleFor(x => x.ArrivalTimeUtc).NotEmpty();
            RuleFor(x => x.AircraftId).NotEmpty();
            RuleFor(x => x.Status).IsEnumName(typeof(FlightStatus));
            RuleFor(x => x.DepartureGate).NotEmpty().MaximumLength(10);
            RuleFor(x => x.ArrivalGate).NotEmpty().MaximumLength(10);
            RuleFor(x => x.DepartureTerminal).NotEmpty().MaximumLength(10);
            RuleFor(x => x.ArrivalTerminal).NotEmpty().MaximumLength(10);
            RuleFor(x => x.AdultMen).NotEmpty().GreaterThanOrEqualTo((short)0);
            RuleFor(x => x.AdultWomen).NotEmpty().GreaterThanOrEqualTo((short)0);
            RuleFor(x => x.Children).NotEmpty().GreaterThanOrEqualTo((short)0);
            RuleFor(x => x.CheckedBags).NotEmpty().GreaterThanOrEqualTo((short)0);
            RuleFor(x => x.Notes).MaximumLength(500);
        }
        private static bool IsValidTimeZoneId(string x)
        {
            try
            {
                var tzi = TimeZoneInfo.FindSystemTimeZoneById(x);
                if (tzi is null)
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    internal sealed class Handler : IRequestHandler<Command, ErrorOr<FlightResponse>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ILogger<Handler> _logger;
        private readonly IValidator<AddFlightRequest> _validator;
        public Handler(ApplicationDbContext ctx, ILogger<Handler> logger,
            IValidator<AddFlightRequest> validator)
        {
            _ctx = ctx;
            _logger = logger;
            _validator = validator;
        }
        public async Task<ErrorOr<FlightResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request.Request, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for AddFlightRequest");
                return Error.Validation(validationResult.Errors[0].ErrorMessage);
            }
            var flight = await MapAddFlightRequestToFlightAsync(request.Request);
            if (flight.IsError)
            {
                _logger.LogWarning("Failed to map AddFlightRequest to Flight: {Error}", flight.FirstError.Description);
                return flight.FirstError;
            }
            _ctx.Flights.Add(flight.Value);
            if (await _ctx.SaveChangesAsync(cancellationToken) == 0)
            {
                _logger.LogWarning("Failed to add flight to database");
                return Error.Failure("Failed to add flight to database");
            }
            return new FlightMapper().MapFlightToFlightResponse(flight.Value);
        }
        private async Task<ErrorOr<Flight>> MapAddFlightRequestToFlightAsync(AddFlightRequest request)
        {
            var aircraft = await _ctx.Aircraft.SingleOrDefaultAsync(x => x.Id == request.AircraftId);
            if (aircraft is null)
            {
                return Error.NotFound($"Aircraft with id {request.AircraftId} not found");
            }
            var departureAirport = await _ctx.Airports.SingleOrDefaultAsync(x => x.Id == request.DepartureAirportId);
            if (departureAirport is null)
            {
                return Error.NotFound($"Departure airport with id {request.DepartureAirportId} not found");
            }
            var arrivalAirport = await _ctx.Airports.SingleOrDefaultAsync(x => x.Id == request.ArrivalAirportId);
            if (arrivalAirport is null)
            {
                return Error.NotFound($"Arrival airport with id {request.ArrivalAirportId} not found");
            }
            var args = new FlightCreationArgs
            {
                Number = request.Number,
                NumberIataFormat = request.NumberIataFormat,
                NumberIcaoFormat = request.NumberIcaoFormat,
                DepartureTimeUtc = request.DepartureTimeUtc,
                ArrivalTimeUtc = request.ArrivalTimeUtc,
                Aircraft = aircraft,
                Status = Enum.Parse<FlightStatus>(request.Status),
                DepartureGate = request.DepartureGate,
                ArrivalGate = request.ArrivalGate,
                DepartureTerminal = request.DepartureTerminal,
                ArrivalTerminal = request.ArrivalTerminal,
                DepartureAirport = departureAirport,
                ArrivalAirport = arrivalAirport,
                AdultMen = request.AdultMen,
                AdultWomen = request.AdultWomen,
                Children = request.Children,
                CheckedBags = request.CheckedBags,
                Notes = request.Notes
            };
            return Flight.Create(args);
        }
    }
}
public class AddFlightEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/flights", async (AddFlightRequest request,
            ISender sender, IOutputCacheStore cache, CancellationToken ct) =>
        {
            var command = new AddFlight.Command(request);
            var result = await sender.Send(command, ct);
            if (!result.IsError)
            {
                await cache.EvictByTagAsync("flights", ct);
            }
            return result.Match(
                flight => Results.Created($"/api/flights/{flight.Id}", flight),
                errors => ErrorHandlingHelper.HandleProblems(errors));
        }).WithName("Add flight")
        .WithOpenApi();
    }
}
