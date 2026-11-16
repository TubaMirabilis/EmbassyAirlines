using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Constructs;
using Deployment.Networking;

namespace Deployment.Lambdas;

internal sealed class DatabaseBackedDockerLambda : Construct
{
    internal DockerImageFunction Function { get; }
    internal DatabaseBackedDockerLambda(
        Construct scope,
        string id,
        DatabaseBackedLambdaProps props,
        Duration timeout,
        DockerImageCode? code = null) : base(scope, id)
    {
        var securityGroup = new SecurityGroup(
            this, "SecurityGroup", new SecurityGroupProps
            {
                Vpc = props.Vpc,
                AllowAllOutbound = true,
                Description = props.SecurityGroupDescription
            });
        securityGroup.Connections.AllowTo(new SecurityGroupConnection
        {
            Description = "Allow Lambda to access RDS Proxy",
            Other = props.DbProxyAccess.DbProxySecurityGroup,
            PortRange = Port.Tcp(props.DbConnection.DbPort)
        });
        securityGroup.Connections.AllowTo(new SecurityGroupConnection
        {
            Description = "Allow Lambda to access AWS interface VPC endpoints",
            Other = props.VpcEndpointSecurityGroup,
            PortRange = Port.Tcp(443)
        });
        var logGroup = new LogGroup(this, "LogGroup", new LogGroupProps
        {
            LogGroupName = $"/aws/lambda/{props.FunctionName}"
        });
        var environment = new Dictionary<string, string>(props.Environment)
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4318",
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
            ["OPENTELEMETRY_COLLECTOR_CONFIG_FILE"] = "/opt/collector-config/config.yaml"
        };
        Function = new DockerImageFunction(this, "Function", new DockerImageFunctionProps
        {
            Code = code ?? DockerImageCode.FromImageAsset(".", new AssetImageCodeProps
            {
                File = props.DockerfilePath
            }),
            Environment = environment,
            FunctionName = props.FunctionName,
            LogGroup = logGroup,
            MemorySize = 1536,
            SecurityGroups = [securityGroup],
            Timeout = timeout,
            Tracing = Tracing.ACTIVE,
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            }
        });
        props.DbProxyAccess.DbProxy.GrantConnect(
            Function,
            props.DbConnection.DbUsername);
    }
}
