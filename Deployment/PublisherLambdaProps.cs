using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.SNS;

namespace Deployment;

internal sealed record PublisherLambdaProps
{
    internal required DatabaseConnectionProps DbConnection { get; init; }
    internal required DatabaseProxyAccessProps DbProxyAccess { get; init; }
    internal required string DockerfilePath { get; init; }
    internal required Dictionary<string, string> Environment { get; init; }
    internal required string FunctionName { get; init; }
    internal required Duration PollInterval { get; init; }
    internal required string SecurityGroupDescription { get; init; }
    internal required Topic[] Topics { get; init; }
    internal required Vpc Vpc { get; init; }
}
