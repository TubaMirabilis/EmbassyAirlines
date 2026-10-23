using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Deployment.Database;

namespace Deployment.Lambdas;

internal sealed record HttpDockerLambdaProps : DatabaseBackedLambdaProps
{
    internal required HttpApi Api { get; init; }
    internal required string RoutePath { get; init; }
}
