using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;

namespace Deployment;

internal sealed record HttpDockerLambdaProps
{
    internal required HttpApi Api { get; init; }
    internal required DatabaseConnectionProps DbConnection { get; init; }
    internal required DatabaseProxyAccessProps DbProxyAccess { get; init; }
    internal required string DockerfilePath { get; init; }
    internal required IReadOnlyDictionary<string, string> Environment { get; init; }
    internal required string FunctionName { get; init; }
    internal required string RoutePath { get; init; }
    internal required string SecurityGroupDescription { get; init; }
    internal required Vpc Vpc { get; init; }
}
