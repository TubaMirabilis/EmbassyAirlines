using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
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
        var dlqAlarm = new Alarm(this, "DlqAlarm", new AlarmProps
        {
            AlarmName = $"{props.FunctionName}-dlq-not-empty",
            AlarmDescription = $"Messages detected in DLQ for {props.FunctionName}",
            Metric = dlq.MetricApproximateNumberOfMessagesVisible(new MetricOptions
            {
                Period = Duration.Minutes(1),
                Statistic = "Maximum"
            }),
            Threshold = 1,
            EvaluationPeriods = 1,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD,
            TreatMissingData = TreatMissingData.NOT_BREACHING
        });
        dlqAlarm.AddAlarmAction(new SnsAction(props.AlarmTopic));
        var queue = new Queue(this, "Queue", new QueueProps
        {
            DeadLetterQueue = new DeadLetterQueue
            {
                MaxReceiveCount = 3,
                Queue = dlq
            },
            VisibilityTimeout = Duration.Seconds(180)
        });
        var queueAgeAlarm = new Alarm(this, "QueueAgeAlarm", new AlarmProps
        {
            AlarmName = $"{props.FunctionName}-queue-age",
            Metric = queue.MetricApproximateAgeOfOldestMessage(new MetricOptions
            {
                Period = Duration.Minutes(1),
                Statistic = "Maximum"
            }),
            Threshold = 300,
            EvaluationPeriods = 3,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
            TreatMissingData = TreatMissingData.NOT_BREACHING
        });
        queueAgeAlarm.AddAlarmAction(new SnsAction(props.AlarmTopic));
        props.Topic.AddSubscription(new SqsSubscription(queue));
        Function.AddEventSource(new SqsEventSource(queue, new SqsEventSourceProps
        {
            BatchSize = 10,
            ReportBatchItemFailures = true
        }));
        queue.GrantConsumeMessages(Function);
    }
}
