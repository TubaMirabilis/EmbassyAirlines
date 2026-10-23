using Amazon.CDK.AWS.SNS;

namespace Deployment.Services;

internal sealed record FlightsServiceProps : ServiceProps
{
    internal required Topic AircraftCreatedTopic { get; init; }
    internal required Topic AirportCreatedTopic { get; init; }
    internal required Topic AirportUpdatedTopic { get; init; }
    internal required Topic FlightScheduledTopic { get; init; }
    internal required Topic AircraftAssignedToFlightTopic { get; init; }
    internal required Topic FlightPricingAdjustedTopic { get; init; }
    internal required Topic FlightRescheduledTopic { get; init; }
    internal required Topic FlightCancelledTopic { get; init; }
    internal required Topic FlightDelayedTopic { get; init; }
    internal required Topic FlightMarkedAsEnRouteTopic { get; init; }
    internal required Topic FlightMarkedAsDelayedEnRouteTopic { get; init; }
    internal required Topic FlightArrivedTopic { get; init; }
}
