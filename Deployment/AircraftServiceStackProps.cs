using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;

namespace Deployment;

internal sealed class AircraftServiceStackProps : StackProps
{
    internal required HttpApi Api { get; init; }
    internal required string EnvironmentName { get; init; }
    internal required Vpc Vpc { get; init; }
}
