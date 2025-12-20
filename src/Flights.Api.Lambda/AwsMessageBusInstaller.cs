using Shared.Contracts;

namespace Flights.Api.Lambda;

internal static class AwsMessageBusInstaller
{
    public static IServiceCollection AddAWSMessageBus(this IServiceCollection services, IConfiguration config) => services.AddAWSMessageBus(bus =>
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
}
