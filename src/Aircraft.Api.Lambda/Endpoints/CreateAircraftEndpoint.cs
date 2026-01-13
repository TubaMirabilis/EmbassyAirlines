using System.Net;
using System.Text.Json;
using Aircraft.Core.Models;
using Aircraft.Infrastructure.Database;
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
    private static async Task<IResult> InvokeAsync(IAmazonS3 client,
                                            IConfiguration config,
                                            ApplicationDbContext ctx,
                                            ILogger<CreateAircraftEndpoint> logger,
                                            IMessagePublisher publisher,
                                            IValidator<CreateAircraftDto> validator,
                                            TimeProvider timeProvider,
                                            CreateAircraftDto dto,
                                            CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid(out var formattedErrors))
        {
            logger.LogWarning("Validation failed for creation of aircraft: {Errors}", formattedErrors);
            var error = Error.Validation("Aircraft.ValidationFailed", formattedErrors);
            return ErrorHandlingHelper.HandleProblem(error);
        }
        if (!Enum.TryParse<Status>(dto.Status, out var status))
        {
            logger.LogWarning("Invalid status value: {Status}", dto.Status);
            var error = Error.Validation("Aircraft.InvalidStatus", $"Invalid status value: {dto.Status}");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        if (await ctx.Aircraft.AnyAsync(a => a.TailNumber == dto.TailNumber, ct))
        {
            logger.LogWarning("Aircraft with tail number {TailNumber} already exists", dto.TailNumber);
            var error = Error.Conflict("Aircraft.TailNumberDuplicate", $"Aircraft with tail number {dto.TailNumber} already exists");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var equipmentCode = dto.EquipmentCode;
        try
        {
            var bucketName = config["S3:BucketName"];
            if (string.IsNullOrEmpty(bucketName))
            {
                logger.LogWarning("Bucket name is not configured");
                var error = Error.Validation("Aircraft.BucketMalfunction", "Bucket name is not configured");
                return ErrorHandlingHelper.HandleProblem(error);
            }
            var response = await client.GetObjectAsync(bucketName, $"seat-layouts/{equipmentCode}.json", ct);
            var def = await JsonSerializer.DeserializeAsync<SeatLayoutDefinition>(response.ResponseStream, cancellationToken: ct);
            if (def is null)
            {
                logger.LogWarning("Seat layout definition for {EquipmentCode} is null", equipmentCode);
                var error = Error.Validation("Aircraft.SeatLayoutDefinitionMalfunction", $"Seat layout definition for {equipmentCode} is null");
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
                CreatedAt = timeProvider.GetUtcNow(),
                Seats = def,
                AircraftLocationData = new AircraftLocationData(status, dto.ParkedAt, dto.EnRouteTo)
            };
            var aircraft = Core.Models.Aircraft.Create(args);
            ctx.Aircraft.Add(aircraft);
            await ctx.SaveChangesAsync(ct);
            await publisher.PublishAsync(new AircraftCreatedEvent(Guid.NewGuid(), aircraft.Id, aircraft.TailNumber, aircraft.EquipmentCode), ct);
            return TypedResults.Created($"/aircraft/{aircraft.Id}", aircraft.ToDto());
        }
        catch (ArgumentException e)
        {
            logger.LogError(e, "Error creating aircraft with tail number {TailNumber}", dto.TailNumber);
            var error = Error.Validation("Aircraft.CreationFailed", $"Error creating aircraft: {e.Message}");
            return ErrorHandlingHelper.HandleProblem(error);
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
