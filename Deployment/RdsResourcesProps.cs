using Amazon.CDK.AWS.EC2;

namespace Deployment;

internal sealed record RdsResourcesProps
{
    internal required string DbName { get; init; }
    internal required int DbPort { get; init; }
    internal required string DbUsername { get; init; }
    internal required Vpc Vpc { get; init; }
}
