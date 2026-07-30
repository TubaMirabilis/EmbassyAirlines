using Amazon.CDK.AWS.EC2;
using Deployment.Database;

namespace Deployment.Lambdas;

internal sealed record DatabaseMigrationLambdaProps
{
    internal required DatabaseConnectionProps DbConnection { get; init; }
    internal required DatabaseProxyAccessProps DbProxyAccess { get; init; }
    internal required string DockerfilePath { get; init; }
    internal required Dictionary<string, string> Environment { get; init; }
    internal required string FunctionName { get; init; }
    internal required string SecurityGroupDescription { get; init; }
    internal required Vpc Vpc { get; init; }
}
