using Amazon.CDK.AWS.SNS;

namespace Deployment.Services;

internal sealed record AircraftServiceProps : ServiceProps
{
    internal required Topic AircraftCreatedTopic { get; init; }
    internal required Topic FlightArrivedTopic { get; init; }
    internal required Topic FlightMarkedAsDelayedEnRouteTopic { get; init; }
    internal required Topic FlightMarkedAsEnRouteTopic { get; init; }
}
