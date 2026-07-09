using Amazon.CDK.AWS.EC2;

namespace Deployment.Database;

internal sealed record RdsResourcesProps
{
    internal required DatabaseConnectionProps DatabaseConnection { get; init; }
    internal required Vpc Vpc { get; init; }
}
