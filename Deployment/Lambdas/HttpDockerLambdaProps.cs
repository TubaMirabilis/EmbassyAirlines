using Amazon.CDK.AWS.Apigatewayv2;

namespace Deployment.Lambdas;

internal sealed record HttpDockerLambdaProps : DatabaseBackedLambdaProps
{
    internal required HttpApi Api { get; init; }
    internal required string RoutePath { get; init; }
}
