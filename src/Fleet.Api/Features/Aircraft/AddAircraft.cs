using Carter;
using ErrorOr;
using Fleet.Api.Contracts;
using Fleet.Api.Database;
using Fleet.Api.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Shared;

namespace Fleet.Api.Features.Aircraft;

public static class AddAircraft
{
    public sealed record Command(AddAircraftRequest Request) : IRequest<ErrorOr<AircraftResponse>>;
    public sealed class AddAircraftRequestValidator : AbstractValidator<AddAircraftRequest>
    {
        public AddAircraftRequestValidator()
        {
            RuleFor(x => x.Registration).NotEmpty().MaximumLength(12);
            RuleFor(x => x.AircraftStatus).IsEnumName(typeof(AircraftStatus));
            RuleFor(x => x.OperationalStatus).IsEnumName(typeof(OperationalStatus));
            RuleFor(x => x.Location).NotEmpty().MaximumLength(4);
            RuleFor(x => x.Model).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Type).IsEnumName(typeof(AircraftType));
            RuleFor(x => x.TypeDesignator).NotEmpty().MaximumLength(4);
            RuleFor(x => x.EngineModel).NotEmpty().MaximumLength(50);
            RuleFor(x => x.SeatingConfiguration).NotEmpty();
            RuleFor(x => x.Wingspan).GreaterThan((short)0);
            RuleFor(x => x.EngineCount).GreaterThan((byte)0);
            RuleFor(x => x.ServiceCeiling).GreaterThan(0);
            RuleFor(x => x.FlightHours).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ProductionDate).NotEmpty();
            RuleFor(x => x.BasicEmptyWeight).GreaterThan(0);
            RuleFor(x => x.MaximumZeroFuelWeight).GreaterThan(0);
            RuleFor(x => x.MaximumTakeoffWeight).GreaterThan(0);
            RuleFor(x => x.MaximumLandingWeight).GreaterThan(0);
            RuleFor(x => x.MaximumCargoWeight).GreaterThanOrEqualTo(0);
            RuleFor(x => x.FuelOnboard).GreaterThanOrEqualTo(0);
            RuleFor(x => x.FuelCapacity).GreaterThan(0);
            RuleFor(x => x.MinimumCabinCrew).GreaterThanOrEqualTo((byte)0);
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
