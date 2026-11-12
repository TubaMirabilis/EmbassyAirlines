using System.Net;
using System.Text.Json;
using Aircraft.Core.Models;
using Aircraft.Infrastructure.Database;
using Amazon.S3;
using ErrorOr;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts;
using Shared.HttpApiLambdaDefaults;
using Shared.HttpApiLambdaDefaults.Extensions;

namespace Aircraft.Api.Lambda.Endpoints;

internal sealed class CreateAircraftEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("aircraft", InvokeAsync)
              .WithSummary("Create an aircraft")
              .Accepts<CreateAircraftDto>("application/json")
              .Produces<AircraftDto>(StatusCodes.Status201Created)
              .ProducesProblem(StatusCodes.Status400BadRequest)
              .ProducesProblem(StatusCodes.Status404NotFound)
              .ProducesProblem(StatusCodes.Status409Conflict)
              .ProducesProblem(StatusCodes.Status500InternalServerError);
    private static async Task<Results<Created<AircraftDto>, ProblemHttpResult>> InvokeAsync(IAmazonS3 client,
                                                                                            IConfiguration config,
                                                                                            ApplicationDbContext ctx,
                                                                                            ILogger<CreateAircraftEndpoint> logger,
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
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        if (await ctx.Aircraft.AsNoTracking().AnyAsync(a => a.TailNumber == dto.TailNumber, ct))
        {
            logger.LogWarning("Aircraft with tail number {TailNumber} already exists", dto.TailNumber);
            var error = Error.Conflict("Aircraft.TailNumberDuplicate", $"Aircraft with tail number {dto.TailNumber} already exists");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
        var equipmentCode = dto.EquipmentCode;
        try
        {
            var bucketName = config["S3:BucketName"];
            if (string.IsNullOrEmpty(bucketName))
            {
                logger.LogWarning("Bucket name is not configured");
                var error = Error.Validation("Aircraft.BucketMalfunction", "Bucket name is not configured");
                return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
            }
            var response = await client.GetObjectAsync(bucketName, $"seat-layouts/{equipmentCode}.json", ct);
            var def = await JsonSerializer.DeserializeAsync<SeatLayoutDefinition>(response.ResponseStream, cancellationToken: ct);
            if (def is null)
            {
                logger.LogWarning("Seat layout definition for {EquipmentCode} is null", equipmentCode);
                var error = Error.Validation("Aircraft.SeatLayoutDefinitionMalfunction", $"Seat layout definition for {equipmentCode} is null");
                return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
            }
            var createdAt = timeProvider.GetUtcNow();
            var args = new AircraftCreationArgs
            {
                CreatedAt = createdAt,
                DryOperatingWeight = new Weight(dto.DryOperatingWeight),
                EquipmentCode = equipmentCode,
                InitialBlockTime = dto.InitialBlockTime,
                MaximumFuelWeight = new Weight(dto.MaximumFuelWeight),
                MaximumLandingWeight = new Weight(dto.MaximumLandingWeight),
                MaximumTakeoffWeight = new Weight(dto.MaximumTakeoffWeight),
                MaximumZeroFuelWeight = new Weight(dto.MaximumZeroFuelWeight),
                Seats = def,
                Situation = dto.Situation.ToDomain(createdAt),
                TailNumber = dto.TailNumber
            };
            var aircraft = Core.Models.Aircraft.Create(args);
            ctx.Aircraft.Add(aircraft);
            await ctx.SaveChangesAsync(ct);
            return TypedResults.Created($"/aircraft/{aircraft.Id}", aircraft.ToDto());
        }
        catch (AmazonS3Exception e)
        {
            logger.LogError(e, "Error retrieving seat layout definition for {EquipmentCode}", equipmentCode);
            var error = e.StatusCode == HttpStatusCode.NotFound
                ? Error.NotFound("Aircraft.SeatLayoutDefinitionNotFound", $"Seat layout definition for {equipmentCode} not found")
                : Error.Failure("Aircraft.SeatLayoutDefinitionRetrievalFailed", $"Error retrieving seat layout definition for {equipmentCode}: {e.Message}");
            return TypedResults.Problem(ErrorHandlingHelper.GetProblemDetails(error));
        }
    }
}
