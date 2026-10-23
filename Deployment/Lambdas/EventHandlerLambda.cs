using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace Deployment.Lambdas;

internal sealed class EventHandlerLambda : Construct
{
    internal DockerImageFunction Function { get; }
    internal EventHandlerLambda(
        Construct scope,
        string id,
        EventHandlerLambdaProps props) : base(scope, id)
    {
        var lambda = new DatabaseBackedDockerLambda(
            this,
            "Lambda",
            props,
            Duration.Seconds(30));
        Function = lambda.Function;
        var dlq = new Queue(this, "Dlq", new QueueProps
        {
            RetentionPeriod = Duration.Days(14)
        });
        var queue = new Queue(this, "Queue", new QueueProps
        {
            DeadLetterQueue = new DeadLetterQueue
            {
                MaxReceiveCount = 3,
                Queue = dlq
            },
            VisibilityTimeout = Duration.Seconds(180)
        });
        props.Topic.AddSubscription(new SqsSubscription(queue));
        Function.AddEventSource(
            new SqsEventSource(queue, new SqsEventSourceProps
            {
                BatchSize = 10,
                ReportBatchItemFailures = true
            }));
        queue.GrantConsumeMessages(Function);
    }
}
