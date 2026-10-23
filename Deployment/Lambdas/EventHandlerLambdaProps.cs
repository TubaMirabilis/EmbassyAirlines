using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.SNS;
using Deployment.Database;

namespace Deployment.Lambdas;

internal sealed record EventHandlerLambdaProps : DatabaseBackedLambdaProps
{
    internal required Topic Topic { get; init; }
}
