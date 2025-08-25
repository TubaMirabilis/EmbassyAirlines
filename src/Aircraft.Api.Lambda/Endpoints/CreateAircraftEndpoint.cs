using System.Net;
using System.Text.Json;
using Aircraft.Api.Lambda.Database;
using Amazon.S3;
using ErrorOr;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Shared.Extensions;

namespace Aircraft.Api.Lambda.Endpoints;

internal sealed class CreateAircraftEndpoint : IEndpoint
{
    private readonly IBus _bus;
    private readonly IAmazonS3 _client;
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _factory;
    private readonly IValidator<CreateOrUpdateAircraftDto> _validator;
    private readonly ILogger<CreateAircraftEndpoint> _logger;
    public CreateAircraftEndpoint(IBus bus, IAmazonS3 client, IConfiguration config, IServiceScopeFactory factory, IValidator<CreateOrUpdateAircraftDto> validator, ILogger<CreateAircraftEndpoint> logger)
    {
        _bus = bus;
        _client = client;
        _config = config;
        _factory = factory;
        _validator = validator;
        _logger = logger;
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("aircraft", InvokeAsync);
    private async Task<IResult> InvokeAsync(CreateOrUpdateAircraftDto dto, CancellationToken ct)
    {
        var validationResult = await _validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            _logger.LogWarning("Validation failed for creation of aircraft: {Errors}", formattedErrors);
            var error = Error.Validation("Aircraft.Validation", formattedErrors);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        using var scope = _factory.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await ctx.Aircraft.AnyAsync(a => a.TailNumber == dto.TailNumber, ct))
        {
            _logger.LogWarning("Aircraft with tail number {TailNumber} already exists", dto.TailNumber);
            var error = Error.Conflict("Aircraft.TailNumber", $"Aircraft with tail number {dto.TailNumber} already exists");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var equipmentCode = dto.EquipmentCode;
        try
        {
            var bucketName = _config["AWS:BucketName"];
            if (string.IsNullOrEmpty(bucketName))
            {
                _logger.LogWarning("Bucket name is not configured");
                var error = Error.Validation("Aircraft.BucketName", "Bucket name is not configured");
                return ErrorHandlingHelper.HandleProblem(error);
            }
            var response = await _client.GetObjectAsync(bucketName, $"seat-layouts/{equipmentCode}.json", ct);
            var def = await JsonSerializer.DeserializeAsync<SeatLayoutDefinition>(response.ResponseStream, cancellationToken: ct);
            if (def is null)
            {
                _logger.LogWarning("Seat layout definition for {EquipmentCode} is null", equipmentCode);
                var error = Error.Validation("Aircraft.SeatLayoutDefinition", $"Seat layout definition for {equipmentCode} is null");
                return ErrorHandlingHelper.HandleProblem(error);
            }
            var args = new AircraftCreationArgs
            {
                TailNumber = dto.TailNumber,
                EquipmentCode = equipmentCode,
                DryOperatingWeight = new Weight(dto.DryOperatingWeight),
                MaximumTakeoffWeight = new Weight(dto.MaximumTakeoffWeight),
                MaximumLandingWeight = new Weight(dto.MaximumLandingWeight),
                MaximumZeroFuelWeight = new Weight(dto.MaximumZeroFuelWeight),
                MaximumFuelWeight = new Weight(dto.MaximumFuelWeight),
                Seats = def
            };
            var aircraft = Aircraft.Create(args);
            ctx.Aircraft.Add(aircraft);
            await ctx.SaveChangesAsync(ct);
            await _bus.Publish(new AircraftCreatedEvent(aircraft.Id, aircraft.TailNumber, aircraft.EquipmentCode), ct);
            return TypedResults.Created($"/aircraft/{aircraft.Id}", aircraft.ToDto());
        }
        catch (AmazonS3Exception e)
        {
            _logger.LogError(e, "Error retrieving seat layout definition for {EquipmentCode}", equipmentCode);
            var error = e.StatusCode == HttpStatusCode.NotFound
                ? Error.Validation("Aircraft.SeatLayoutDefinition", $"Seat layout definition for {equipmentCode} not found")
                : Error.Failure("Aircraft.SeatLayoutDefinition", $"Error retrieving seat layout definition for {equipmentCode}: {e.Message}");
            return ErrorHandlingHelper.HandleProblem(error);
        }
    }
}
