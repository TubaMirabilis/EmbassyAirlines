using Amazon.CDK.AWS.EC2;

namespace Deployment;

internal sealed record RdsResourcesProps
{
    internal required Vpc Vpc { get; init; }
}
