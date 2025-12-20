using Flights.Api.Lambda;
using Flights.Infrastructure;
using FluentValidation;
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
services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
services.AddExceptionHandler<GlobalExceptionHandler>();
services.AddProblemDetails();
if (!builder.Environment.IsEnvironment("FunctionalTests"))
{
    services.AddDatabaseConnection(config);
}
services.AddSingleton<IValidator<CreateOrUpdateFlightDto>, CreateOrUpdateFlightDtoValidator>();
services.AddOpenApi();
builder.Services.AddAWSMessageBus(bus =>
{
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
    var flightScheduledTopicArn = config["SNS:FlightScheduledTopicArn"];
    if (string.IsNullOrWhiteSpace(flightScheduledTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for FlightScheduledEvent is not configured.");
    }
    var flightCancelledTopicArn = config["SNS:FlightCancelledTopicArn"];
    if (string.IsNullOrWhiteSpace(flightCancelledTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for FlightCancelledEvent is not configured.");
    }
    var flightArrivedTopicArn = config["SNS:FlightArrivedTopicArn"];
    if (string.IsNullOrWhiteSpace(flightArrivedTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for FlightArrivedEvent is not configured.");
    }
    var flightDelayedTopicArn = config["SNS:FlightDelayedTopicArn"];
    if (string.IsNullOrWhiteSpace(flightDelayedTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for FlightDelayedEvent is not configured.");
    }
    var flightEnRouteTopicArn = config["SNS:FlightEnRouteTopicArn"];
    if (string.IsNullOrWhiteSpace(flightEnRouteTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for FlightEnRouteEvent is not configured.");
    }
    var flightDelayedEnRouteTopicArn = config["SNS:FlightDelayedEnRouteTopicArn"];
    if (string.IsNullOrWhiteSpace(flightDelayedEnRouteTopicArn))
    {
        throw new InvalidOperationException("SNS Topic ARN for FlightDelayedEnRouteEvent is not configured.");
    }
    bus.AddSNSPublisher<AircraftAssignedToFlightEvent>(aircraftAssignedTopicArn);
    bus.AddSNSPublisher<FlightPricingAdjustedEvent>(flightPricingAdjustedTopicArn);
    bus.AddSNSPublisher<FlightRescheduledEvent>(flightRescheduledTopicArn);
    bus.AddSNSPublisher<FlightScheduledEvent>(flightScheduledTopicArn);
    bus.AddSNSPublisher<FlightCancelledEvent>(flightCancelledTopicArn);
    bus.AddSNSPublisher<FlightArrivedEvent>(flightArrivedTopicArn);
    bus.AddSNSPublisher<FlightDelayedEvent>(flightDelayedTopicArn);
    bus.AddSNSPublisher<FlightEnRouteEvent>(flightEnRouteTopicArn);
    bus.AddSNSPublisher<FlightDelayedEnRouteEvent>(flightDelayedEnRouteTopicArn);
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
await app.ApplyMigrationsAsync();
app.MapEndpoints();
app.UseMiddleware<RequestContextLoggingMiddleware>();
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
await app.RunAsync();

#pragma warning disable CA1515
public partial class Program { }
