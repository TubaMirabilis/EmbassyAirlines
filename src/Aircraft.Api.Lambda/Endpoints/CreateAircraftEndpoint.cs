using System.Net;
using System.Text.Json;
using Aircraft.Api.Lambda.Database;
using Amazon.S3;
using AWS.Messaging;
using ErrorOr;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Contracts;
using Shared.Endpoints;
using Shared.Extensions;

namespace Aircraft.Api.Lambda.Endpoints;

internal sealed class CreateAircraftEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("aircraft", InvokeAsync);
    private async Task<IResult> InvokeAsync(IAmazonS3 client, IConfiguration config, ApplicationDbContext ctx, ILogger<CreateAircraftEndpoint> logger, IMessagePublisher publisher, IValidator<CreateOrUpdateAircraftDto> validator, CreateOrUpdateAircraftDto dto, CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            logger.LogWarning("Validation failed for creation of aircraft: {Errors}", formattedErrors);
            var error = Error.Validation("Aircraft.Validation", formattedErrors);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        if (await ctx.Aircraft.AnyAsync(a => a.TailNumber == dto.TailNumber, ct))
        {
            logger.LogWarning("Aircraft with tail number {TailNumber} already exists", dto.TailNumber);
            var error = Error.Conflict("Aircraft.TailNumber", $"Aircraft with tail number {dto.TailNumber} already exists");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var equipmentCode = dto.EquipmentCode;
        try
        {
            var bucketName = config["S3:BucketName"];
            if (string.IsNullOrEmpty(bucketName))
            {
                logger.LogWarning("Bucket name is not configured");
                var error = Error.Validation("Aircraft.BucketName", "Bucket name is not configured");
                return ErrorHandlingHelper.HandleProblem(error);
            }
            var response = await client.GetObjectAsync(bucketName, $"seat-layouts/{equipmentCode}.json", ct);
            var def = await JsonSerializer.DeserializeAsync<SeatLayoutDefinition>(response.ResponseStream, cancellationToken: ct);
            if (def is null)
            {
                logger.LogWarning("Seat layout definition for {EquipmentCode} is null", equipmentCode);
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
            await publisher.PublishAsync(new AircraftCreatedEvent(aircraft.Id, aircraft.TailNumber, aircraft.EquipmentCode), ct);
            return TypedResults.Created($"/aircraft/{aircraft.Id}", aircraft.ToDto());
        }
        catch (AmazonS3Exception e)
        {
            logger.LogError(e, "Error retrieving seat layout definition for {EquipmentCode}", equipmentCode);
            var error = e.StatusCode == HttpStatusCode.NotFound
                ? Error.NotFound("Aircraft.SeatLayoutDefinitionNotFound", $"Seat layout definition for {equipmentCode} not found")
                : Error.Failure("Aircraft.SeatLayoutDefinitionRetrievalFailed", $"Error retrieving seat layout definition for {equipmentCode}: {e.Message}");
            return ErrorHandlingHelper.HandleProblem(error);
        }
    }
}
