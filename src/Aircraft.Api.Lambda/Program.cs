using System.Net;
using System.Text.Json;
using Aircraft.Api.Lambda;
using Aircraft.Api.Lambda.Database;
using Amazon.S3;
using ErrorOr;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRCRAFT_");
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(config["ConnectionStrings:DefaultConnection"])
           .UseSnakeCaseNamingConvention());
builder.Services.AddSingleton<IValidator<CreateOrUpdateAircraftDto>, CreateOrUpdateAircraftDtoValidator>();
var app = builder.Build();
app.MapPost("aircraft", async ([FromServices] ApplicationDbContext ctx, IAmazonS3 client, IValidator<CreateOrUpdateAircraftDto> validator, [FromBody] CreateOrUpdateAircraftDto dto) =>
{
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid(out var formattedErrors))
    {
        var error = Error.Validation("Airport.Validation", formattedErrors);
        return ErrorHandlingHelper.HandleProblem(error);
    }
    if (ctx.Aircraft.Any(a => a.TailNumber == dto.TailNumber))
    {
        var error = Error.Conflict("Aircraft.TailNumber", $"Aircraft with tail number {dto.TailNumber} already exists");
        return ErrorHandlingHelper.HandleProblem(error);
    }
    var equipmentCode = dto.EquipmentCode;
    try
    {
        var bucketName = config["AWS:BucketName"];
        if (string.IsNullOrEmpty(bucketName))
        {
            var error = Error.Validation("Aircraft.BucketName", "Bucket name is not configured");
            return ErrorHandlingHelper.HandleProblem(error);
        }
        var response = await client.GetObjectAsync(bucketName, $"{equipmentCode}.json");
        var def = await JsonSerializer.DeserializeAsync<SeatLayoutDefinition>(response.ResponseStream) ?? throw new InvalidOperationException($"Seat layout definition for {equipmentCode} is null");
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
        var aircraft = Aircraft.Api.Lambda.Aircraft.Create(args);
        ctx.Aircraft.Add(aircraft);
        await ctx.SaveChangesAsync();
        return TypedResults.Created($"/aircraft/{aircraft.Id}", aircraft.ToDto());
    }
    catch (AmazonS3Exception e)
    {
        var error = e.StatusCode == HttpStatusCode.NotFound
            ? Error.Validation("Aircraft.SeatLayoutDefinition", $"Seat layout definition for {equipmentCode} not found")
            : Error.Failure("Aircraft.SeatLayoutDefinition", $"Error retrieving seat layout definition for {equipmentCode}: {e.Message}");
        return ErrorHandlingHelper.HandleProblem(error);
    }
    catch (InvalidOperationException e)
    {
        var error = Error.Validation("Aircraft.SeatLayoutDefinition", e.Message);
        return ErrorHandlingHelper.HandleProblem(error);
    }
});
app.UseExceptionHandler();
await app.RunAsync();
