using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;

namespace Deployment;

internal sealed record AircraftServiceProps
{
    internal required HttpApi Api { get; init; }
    internal required DatabaseProxy DbProxy { get; init; }
    internal required SecurityGroup DbProxySecurityGroup { get; init; }
    internal required Vpc Vpc { get; init; }
}
