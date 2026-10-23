using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.SNS;
using Deployment.Database;

namespace Deployment.Lambdas;

internal sealed record PublisherLambdaProps : DatabaseBackedLambdaProps
{
    internal required Duration PollInterval { get; init; }
    internal required Topic[] Topics { get; init; }
}
