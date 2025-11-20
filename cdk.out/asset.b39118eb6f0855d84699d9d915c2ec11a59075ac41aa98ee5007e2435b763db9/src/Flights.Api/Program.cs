using Flights.Api;
using Flights.Api.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared;
using Shared.Contracts;
using Shared.Extensions;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "FLIGHTS_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
var assembly = typeof(Program).Assembly;
var services = builder.Services;
services.AddEndpoints(assembly);
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(config["ConnectionStrings:DefaultConnection"],
               o => o.UseNodaTime())
           .UseSnakeCaseNamingConvention());
services.AddSingleton<IValidator<CreateOrUpdateFlightDto>, CreateOrUpdateFlightDtoValidator>();
services.AddOpenApi();
builder.Services.AddAWSMessageBus(bus =>
{
    var queueUrl = config["SQS:QueueUrl"];
    if (string.IsNullOrWhiteSpace(queueUrl))
    {
        throw new InvalidOperationException("SQS Queue URL is not configured.");
    }
    var aircraftAssignedTopicArn = config["SNS:AircraftAssignedToFlightTopicArn"];
    if (string.IsNullOrWhiteSpace(aircraftAssignedTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for AircraftAssignedToFlightEvent is not configured.");
    }
    var flightPricingAdjustedTopicArn = config["SNS:FlightPricingAdjustedTopicArn"];
    if (string.IsNullOrWhiteSpace(flightPricingAdjustedTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for FlightPricingAdjustedEvent is not configured.");
    }
    var flightRescheduledTopicArn = config["SNS:FlightRescheduledTopicArn"];
    if (string.IsNullOrWhiteSpace(flightRescheduledTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for FlightRescheduledEvent is not configured.");
    }
    bus.AddSQSPoller(queueUrl);
    bus.AddSNSPublisher<AircraftAssignedToFlightEvent>(aircraftAssignedTopicArn);
    bus.AddSNSPublisher<FlightPricingAdjustedEvent>(flightPricingAdjustedTopicArn);
    bus.AddSNSPublisher<FlightRescheduledEvent>(flightRescheduledTopicArn);
    bus.AddMessageHandler<AircraftCreatedEventHandler, AircraftCreatedEvent>();
    bus.AddMessageHandler<AirportCreatedEventHandler, AirportCreatedEvent>();
    bus.AddMessageHandler<AirportUpdatedEventHandler, AirportUpdatedEvent>();
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
