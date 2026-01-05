using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace Deployment;

internal sealed class EventHandlerLambda : Construct
{
    internal EventHandlerLambda(Construct scope, string id, EventHandlerLambdaProps props) : base(scope, id)
    {
        var handlerSg = new SecurityGroup(this, "HandlerSg", new SecurityGroupProps
        {
            AllowAllOutbound = true,
            Description = props.SecurityGroupDescription,
            Vpc = props.Vpc
        });
        handlerSg.Connections.AllowTo(props.DbProxySecurityGroup, Port.Tcp(props.DbPort), "Allow handler Lambda to access RDS Proxy");
        var imageCode = DockerImageCode.FromImageAsset(directory: ".", new AssetImageCodeProps
        {
            File = props.Path
        });
        var handler = new DockerImageFunction(this, "Handler", new DockerImageFunctionProps
        {
            Code = imageCode,
            Environment = props.Environment,
            FunctionName = props.FunctionName,
            MemorySize = 512,
            SecurityGroups = [handlerSg],
            Timeout = Duration.Seconds(30),
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            }
        });
        props.DbProxy.GrantConnect(handler, props.DbUsername);
        var dlq = new Queue(this, "Dlq");
        var queue = new Queue(this, "Queue", new QueueProps
        {
            DeadLetterQueue = new DeadLetterQueue
            {
                MaxReceiveCount = 3,
                Queue = dlq
            },
            VisibilityTimeout = Duration.Seconds(90)
        });
        props.Topic.AddSubscription(new SqsSubscription(queue));
        handler.AddEventSource(new SqsEventSource(queue, new SqsEventSourceProps
        {
            BatchSize = 10,
            ReportBatchItemFailures = true
        }));
        queue.GrantConsumeMessages(handler);
    }
}
