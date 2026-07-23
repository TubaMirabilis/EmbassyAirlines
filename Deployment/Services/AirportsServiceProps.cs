using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.SNS;
using Deployment.Database;

namespace Deployment.Services;

internal sealed record AirportsServiceProps
{
    internal required Topic AirportCreatedTopic { get; init; }
    internal required Topic AirportUpdatedTopic { get; init; }
    internal required HttpApi Api { get; init; }
    internal required DatabaseConnectionProps DbConnection { get; init; }
    internal required DatabaseProxyAccessProps DbProxyAccess { get; init; }
    internal required Vpc Vpc { get; init; }
}
