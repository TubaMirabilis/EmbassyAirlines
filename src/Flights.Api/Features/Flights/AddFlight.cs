using Carter;
using ErrorOr;
using Flights.Api.Contracts;
using Flights.Api.Database;
using Flights.Api.Domain.Flights;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
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
            var mapper = new FlightMapper();
            var flight = mapper.MapAddFlightRequestToFlight(request.Request);
            _ctx.Flights.Add(flight);
            if (await _ctx.SaveChangesAsync(cancellationToken) == 0)
            {
                _logger.LogWarning("Failed to add flight to database");
                return Error.Failure("Failed to add flight to database");
            }
            return mapper.MapFlightToFlightResponse(flight);
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
