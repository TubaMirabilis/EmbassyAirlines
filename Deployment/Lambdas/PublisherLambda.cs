using Amazon.CDK;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace Deployment.Lambdas;

internal sealed class PublisherLambda : Construct
{
    internal DockerImageFunction Function { get; }
    internal PublisherLambda(
        Construct scope,
        string id,
        PublisherLambdaProps props) : base(scope, id)
    {
        var lambda = new DatabaseBackedDockerLambda(
            this,
            "Lambda",
            props,
            Duration.Seconds(60));
        Function = lambda.Function;
        foreach (var topic in props.Topics)
        {
            topic.GrantPublish(Function);
        }
        var rule = new Rule(this, "Schedule", new RuleProps
        {
            Description =
                $"Drains the {props.FunctionName} transactional outbox on a fixed schedule",
            Schedule = Schedule.Rate(props.PollInterval)
        });
        rule.AddTarget(new LambdaFunction(Function));
    }
}
