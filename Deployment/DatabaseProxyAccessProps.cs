using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;

namespace Deployment;

internal sealed record DatabaseProxyAccessProps
{
    internal required DatabaseProxy DbProxy { get; init; }
    internal required SecurityGroup DbProxySecurityGroup { get; init; }
}
