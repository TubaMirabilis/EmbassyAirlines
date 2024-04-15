using ErrorOr;
using Flights.Api.Contracts;
using Flights.Api.Enums;
using FluentValidation;
using MediatR;

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
            RuleFor(x => x.Status).IsEnumName(typeof(Status));
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
    internal sealed class Handler : IRequestHandler<Command, ErrorOr<AircraftResponse>>
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ILogger<Handler> _logger;
        private readonly IValidator<AddAircraftRequest> _validator;
        public Handler(ApplicationDbContext ctx, ILogger<Handler> logger, IValidator<AddAircraftRequest> validator)
        {
            _ctx = ctx;
            _logger = logger;
            _validator = validator;
        }
        public async Task<ErrorOr<AircraftResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Validating AddAircraftRequest");
            var validationResult = await _validator.ValidateAsync(request.Request, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for AddAircraftRequest");
                return Error.Validation(validationResult.Errors[0].ErrorMessage);
            }
            _logger.LogInformation("Mapping AddAircraftRequest to Aircraft");
            var mapper = new AircraftMapper();
            var aircraft = mapper.MapAddAircraftRequestToAircraft(request.Request);
            _logger.LogInformation("Adding aircraft");
            _ctx.Aircraft.Add(aircraft);
            await _ctx.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Aircraft added");
            return mapper.MapAircraftToAircraftResponse(aircraft);
        }
    }
}
public class AddAircraftEndpoint : ICarterModule
{
    private readonly ILogger<AddAircraftEndpoint> _logger;
    public AddAircraftEndpoint(ILogger<AddAircraftEndpoint> logger)
    {
        _logger = logger;
    }
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/aircraft", async (AddAircraftRequest request,
            ISender sender, IOutputCacheStore cache, CancellationToken ct) =>
        {
            _logger.LogInformation("Adding aircraft");
            var command = new AddAircraft.Command(request);
            var result = await sender.Send(command, ct);
            if (!result.IsError)
            {
                await cache.EvictByTagAsync("aircraft", ct);
            }
            return result.Match(
                ac => Results.Created("/api/aircraft", ac),
                errors => ErrorHandlingHelper.HandleProblems(errors));
        }).WithName("Add aircraft")
        .WithOpenApi();
    }
}
