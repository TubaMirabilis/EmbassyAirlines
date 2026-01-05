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
        var handlerCode = Code.FromAsset(props.Path, new Amazon.CDK.AWS.S3.Assets.AssetOptions
        {
            Bundling = new BundlingOptions
            {
                Image = DockerImage.FromRegistry("mcr.microsoft.com/dotnet/sdk:10.0"),
                Command = ["bash", "-lc", "dotnet publish -c Release -o /asset-output"]
            }
        });
        var handler = new Function(this, "Handler", new FunctionProps
        {
            FunctionName = props.FunctionName,
            Runtime = Runtime.DOTNET_10,
            Handler = props.Handler,
            Code = handlerCode,
            Timeout = Duration.Seconds(30),
            MemorySize = 512,
            Environment = props.Environment,
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            },
            SecurityGroups = [handlerSg]
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
