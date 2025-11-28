using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;

namespace Deployment;

internal sealed class AircraftServiceStackProps : StackProps
{
    internal required HttpApi Api { get; init; }
    internal required DatabaseInstance DbInstance { get; init; }
    internal required CfnParameter DbPasswordParam { get; init; }
    internal required CfnParameter DbUser { get; init; }
    internal required Vpc Vpc { get; init; }
}
