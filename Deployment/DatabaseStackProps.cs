using Amazon.CDK;
using Amazon.CDK.AWS.EC2;

namespace Deployment;

internal sealed class DatabaseStackProps : StackProps
{
    internal required Vpc Vpc { get; init; }
}
