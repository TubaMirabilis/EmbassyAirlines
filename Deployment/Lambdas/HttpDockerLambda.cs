using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;
using Deployment.Networking;

namespace Deployment.Lambdas;

internal sealed class HttpDockerLambda : Construct
{
    internal DockerImageFunction Function { get; }
    internal HttpDockerLambda(Construct scope, string id, HttpDockerLambdaProps props) : base(scope, id)
    {
        var securityGroup = new SecurityGroup(this, "LambdaSg", new SecurityGroupProps
        {
            Vpc = props.Vpc,
            AllowAllOutbound = true,
            Description = props.SecurityGroupDescription
        });
        var imageCode = DockerImageCode.FromImageAsset(".", new AssetImageCodeProps
        {
            File = props.DockerfilePath
        });
        var logGroup = new LogGroup(this, "LogGroup", new LogGroupProps
        {
            LogGroupName = $"/aws/lambda/{props.FunctionName}",
        });
        Function = new DockerImageFunction(this, "Lambda", new DockerImageFunctionProps
        {
            Code = imageCode,
            Environment = props.Environment,
            FunctionName = props.FunctionName,
            LogGroup = logGroup,
            MemorySize = 1536,
            SecurityGroups = [securityGroup],
            Timeout = Duration.Seconds(30),
            Tracing = Tracing.ACTIVE,
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            }
        });
        var integration = new HttpLambdaIntegration(
            $"{props.FunctionName}Integration",
            Function);
        props.Api.AddRoutes(new AddRoutesOptions
        {
            Path = props.RoutePath,
            Integration = integration,
            Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY]
        });
        props.Api.AddRoutes(new AddRoutesOptions
        {
            Path = $"{props.RoutePath}/{{proxy+}}",
            Integration = integration,
            Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY]
        });
        props.DbProxyAccess.DbProxy.GrantConnect(Function, props.DbConnection.DbUsername);
        var handlerSgRule = new SecurityGroupConnection
        {
            Description = "Allow Lambda to access RDS Proxy",
            Other = props.DbProxyAccess.DbProxySecurityGroup,
            PortRange = Port.Tcp(props.DbConnection.DbPort)
        };
        securityGroup.Connections.AllowTo(handlerSgRule);
    }
}
