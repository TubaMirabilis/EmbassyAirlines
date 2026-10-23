using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.SNS;
using Deployment.Database;

namespace Deployment.Services;

internal sealed record ServiceProps
{
    internal required HttpApi Api { get; init; }
    internal required DatabaseConnectionProps DbConnection { get; init; }
    internal required DatabaseProxyAccessProps DbProxyAccess { get; init; }
    internal required IReadOnlyDictionary<string, Topic> Topics { get; init; }
    internal required Vpc Vpc { get; init; }
}
