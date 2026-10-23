using Amazon.CDK.AWS.SNS;

namespace Deployment.Lambdas;

internal sealed record EventHandlerLambdaProps : DatabaseBackedLambdaProps
{
    internal required Topic Topic { get; init; }
}
