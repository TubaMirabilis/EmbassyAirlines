using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.SNS;

namespace Deployment;

internal sealed record AircraftServiceProps
{
    internal required Topic AircraftCreatedTopic { get; init; }
    internal required HttpApi Api { get; init; }
    internal required DatabaseConnectionProps DbConnection { get; init; }
    internal required DatabaseProxyAccessProps DbProxyAccess { get; init; }
    internal required Topic FlightArrivedTopic { get; init; }
    internal required Topic FlightMarkedAsDelayedEnRouteTopic { get; init; }
    internal required Topic FlightMarkedAsEnRouteTopic { get; init; }
    internal required Vpc Vpc { get; init; }
}
