using Aircraft.Api.Lambda;
using Aircraft.Api.Lambda.Database;
using Amazon.S3;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared;
using Shared.Contracts;
using Shared.Extensions;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRCRAFT_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var assembly = typeof(Program).Assembly;
builder.Services.AddEndpoints(assembly);
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(config["ConnectionStrings:DefaultConnection"])
           .UseSnakeCaseNamingConvention());
builder.Services.AddSingleton<IValidator<CreateOrUpdateAircraftDto>, CreateOrUpdateAircraftDtoValidator>();
builder.Services.AddOpenApi();
builder.Services.AddAWSMessageBus(bus =>
{
    var aircraftCreatedTopicArn = config["SNS:AircraftCreatedTopicArn"];
    if (string.IsNullOrWhiteSpace(aircraftCreatedTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for AircraftCreatedEvent is not configured.");
    }
    bus.AddSNSPublisher<AircraftCreatedEvent>(aircraftCreatedTopicArn);
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync();
    app.MapOpenApi();
}
app.MapEndpoints();
app.UseMiddleware<RequestContextLoggingMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
#pragma warning restore CA1515
