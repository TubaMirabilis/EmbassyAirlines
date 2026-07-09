using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace Deployment;

internal sealed class PublisherLambda : Construct
{
    internal DockerImageFunction Function { get; }
    internal PublisherLambda(Construct scope, string id, PublisherLambdaProps props) : base(scope, id)
    {
        var publisherSg = new SecurityGroup(this, "PublisherSg", new SecurityGroupProps
        {
            AllowAllOutbound = true,
            Description = props.SecurityGroupDescription,
            Vpc = props.Vpc
        });
        publisherSg.Connections.AllowTo(new ConnectionRule
        {
            Description = "Allow publisher Lambda to access RDS Proxy",
            Other = props.DbProxyAccess.DbProxySecurityGroup,
            PortRange = Port.Tcp(props.DbConnection.DbPort)
        });
        var imageCode = DockerImageCode.FromImageAsset(directory: ".", new AssetImageCodeProps
        {
            File = props.DockerfilePath
        });
        Function = new DockerImageFunction(this, "Publisher", new DockerImageFunctionProps
        {
            Code = imageCode,
            Environment = props.Environment.WithOtel(props.FunctionName),
            FunctionName = props.FunctionName,
            MemorySize = 512,
            SecurityGroups = [publisherSg],
            Timeout = Duration.Seconds(60),
            Tracing = Tracing.ACTIVE,
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            }
        });
        props.DbProxyAccess.DbProxy.GrantConnect(Function, props.DbConnection.DbUsername);
        foreach (var topic in props.Topics)
        {
            topic.GrantPublish(Function);
        }
        var rule = new Rule(this, "Schedule", new RuleProps
        {
            Description = $"Drains the {props.FunctionName} transactional outbox on a fixed schedule",
            Schedule = Schedule.Rate(props.PollInterval)
        });
        rule.AddTarget(new LambdaFunction(Function));
    }
}
