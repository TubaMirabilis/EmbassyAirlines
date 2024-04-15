using Carter;
using ErrorOr;
using Flights.Api.Contracts;
using Flights.Api.Database;
using Flights.Api.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Shared;

namespace Flights.Api.Features.Aircraft;

public static class AddFlight
{
    public sealed record Command(AddOrUpdateFlightRequest Request) : IRequest<ErrorOr<FlightResponse>>;
    public sealed class AddOrUpdateFlightRequestValidator : AbstractValidator<AddOrUpdateFlightRequest>
    {
        public AddOrUpdateFlightRequestValidator()
        {
            RuleFor(x => x.Number).NotEmpty().MaximumLength(10);
            RuleFor(x => x.NumberIataFormat).NotEmpty().MaximumLength(10);
            RuleFor(x => x.NumberIcaoFormat).NotEmpty().MaximumLength(10);
            RuleFor(x => x.DepartureTimeUtc).NotEmpty().LessThan(x => x.ArrivalTimeUtc);
            RuleFor(x => x.ArrivalTimeUtc).NotEmpty();
            RuleFor(x => x.DepartureTimeZoneId).NotEmpty().MaximumLength(50).Must(IsValidTimeZoneId);
            RuleFor(x => x.ArrivalTimeZoneId).NotEmpty().MaximumLength(50).Must(IsValidTimeZoneId);
            RuleFor(x => x.AircraftTypeDesignator).NotEmpty().MaximumLength(4);
            RuleFor(x => x.AircraftRegistration).NotEmpty().MaximumLength(10);
            RuleFor(x => x.Status).IsEnumName(typeof(FlightStatus));
            RuleFor(x => x.DepartureGate).NotEmpty().MaximumLength(10);
            RuleFor(x => x.ArrivalGate).NotEmpty().MaximumLength(10);
            RuleFor(x => x.DepartureTerminal).NotEmpty().MaximumLength(10);
            RuleFor(x => x.ArrivalTerminal).NotEmpty().MaximumLength(10);
            RuleFor(x => x.DepartureAirportIata).NotEmpty().MaximumLength(3);
            RuleFor(x => x.ArrivalAirportIata).NotEmpty().MaximumLength(3);
            RuleFor(x => x.DepartureAirportIcao).NotEmpty().MaximumLength(4);
            RuleFor(x => x.ArrivalAirportIcao).NotEmpty().MaximumLength(4);
            RuleFor(x => x.Distance).NotEmpty().GreaterThan((short)0);
            RuleFor(x => x.AdultMen).NotEmpty().GreaterThanOrEqualTo((short)0);
            RuleFor(x => x.AdultWomen).NotEmpty().GreaterThanOrEqualTo((short)0);
            RuleFor(x => x.Children).NotEmpty().GreaterThanOrEqualTo((short)0);
            RuleFor(x => x.CheckedBags).NotEmpty().GreaterThanOrEqualTo((short)0);
            RuleFor(x => x.Notes).MaximumLength(500);
            RuleFor(x => x.DepartureTaf).MaximumLength(500);
            RuleFor(x => x.ArrivalTaf).MaximumLength(500);
            RuleFor(x => x.DepartureMetar).MaximumLength(500);
            RuleFor(x => x.ArrivalMetar).MaximumLength(500);
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
        private readonly IValidator<AddOrUpdateFlightRequest> _validator;
        public Handler(ApplicationDbContext ctx, ILogger<Handler> logger, IValidator<AddOrUpdateFlightRequest> validator)
        {
            _ctx = ctx;
            _logger = logger;
            _validator = validator;
        }
        public async Task<ErrorOr<FlightResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Validating AddOrUpdateFlightRequest");
            var validationResult = await _validator.ValidateAsync(request.Request, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for AddOrUpdateFlightRequest");
                return Error.Validation(validationResult.Errors[0].ErrorMessage);
            }
            _logger.LogInformation("Mapping AddOrUpdateFlightRequest to Flight");
            var mapper = new FlightMapper();
            var flight = mapper.MapAddOrUpdateFlightRequestToFlight(request.Request);
            _logger.LogInformation("Adding flight to database");
            _ctx.Flights.Add(flight);
            await _ctx.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Flight added to database");
            return mapper.MapFlightToFlightResponse(flight);
        }
    }
}
public class AddFlightEndpoint : ICarterModule
{
    private readonly ILogger<AddFlightEndpoint> _logger;
    public AddFlightEndpoint(ILogger<AddFlightEndpoint> logger)
    {
        _logger = logger;
    }
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/flights", async (AddOrUpdateFlightRequest request,
            ISender sender, IOutputCacheStore cache, CancellationToken ct) =>
        {
            _logger.LogInformation("Received request to add flight");
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
