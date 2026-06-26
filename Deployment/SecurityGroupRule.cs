using Amazon.CDK.AWS.EC2;

namespace Deployment;

internal sealed record ConnectionRule
{
    internal required IConnectable Other { get; init; }
    internal required Port PortRange { get; init; }
    internal required string Description { get; init; }
}
