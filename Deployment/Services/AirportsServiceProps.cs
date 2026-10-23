using Amazon.CDK.AWS.SNS;

namespace Deployment.Services;

internal sealed record AirportsServiceProps : ServiceProps
{
    internal required Topic AirportCreatedTopic { get; init; }
    internal required Topic AirportUpdatedTopic { get; init; }
}
