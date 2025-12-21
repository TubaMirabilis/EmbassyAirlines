using Shared;
using Shared.Contracts;

namespace Flights.Api.Lambda;

internal static class AwsMessageBusInstaller
{
    public static IServiceCollection AddAWSMessageBus(this IServiceCollection services, IConfiguration config) => services.AddAWSMessageBus(bus =>
    {
        var aircraftAssignedTopicArn = config["SNS:AircraftAssignedToFlightTopicArn"];
        var flightPricingAdjustedTopicArn = config["SNS:FlightPricingAdjustedTopicArn"];
        var flightRescheduledTopicArn = config["SNS:FlightRescheduledTopicArn"];
        var flightScheduledTopicArn = config["SNS:FlightScheduledTopicArn"];
        var flightCancelledTopicArn = config["SNS:FlightCancelledTopicArn"];
        var flightArrivedTopicArn = config["SNS:FlightArrivedTopicArn"];
        var flightDelayedTopicArn = config["SNS:FlightDelayedTopicArn"];
        var flightMarkedAsEnRouteTopicArn = config["SNS:FlightMarkedAsEnRouteTopicArn"];
        var flightMarkedAsDelayedEnRouteTopicArn = config["SNS:FlightMarkedAsDelayedEnRouteTopicArn"];
        Ensure.NotNullOrEmpty(aircraftAssignedTopicArn);
        Ensure.NotNullOrEmpty(flightPricingAdjustedTopicArn);
        Ensure.NotNullOrEmpty(flightRescheduledTopicArn);
        Ensure.NotNullOrEmpty(flightScheduledTopicArn);
        Ensure.NotNullOrEmpty(flightCancelledTopicArn);
        Ensure.NotNullOrEmpty(flightArrivedTopicArn);
        Ensure.NotNullOrEmpty(flightDelayedTopicArn);
        Ensure.NotNullOrEmpty(flightMarkedAsEnRouteTopicArn);
        Ensure.NotNullOrEmpty(flightMarkedAsDelayedEnRouteTopicArn);
        bus.AddSNSPublisher<AircraftAssignedToFlightEvent>(aircraftAssignedTopicArn);
        bus.AddSNSPublisher<FlightPricingAdjustedEvent>(flightPricingAdjustedTopicArn);
        bus.AddSNSPublisher<FlightRescheduledEvent>(flightRescheduledTopicArn);
        bus.AddSNSPublisher<FlightScheduledEvent>(flightScheduledTopicArn);
        bus.AddSNSPublisher<FlightCancelledEvent>(flightCancelledTopicArn);
        bus.AddSNSPublisher<FlightArrivedEvent>(flightArrivedTopicArn);
        bus.AddSNSPublisher<FlightDelayedEvent>(flightDelayedTopicArn);
        bus.AddSNSPublisher<FlightMarkedAsEnRouteEvent>(flightMarkedAsEnRouteTopicArn);
        bus.AddSNSPublisher<FlightMarkedAsDelayedEnRouteEvent>(flightMarkedAsDelayedEnRouteTopicArn);
    });
}
