using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;

namespace Deployment;

internal sealed class HttpDockerLambda : Construct
{
    internal DockerImageFunction Function { get; }
    internal HttpDockerLambda(Construct scope, string id, HttpDockerLambdaProps props) : base(scope, id)
    {
        var imageCode = DockerImageCode.FromImageAsset(".", new AssetImageCodeProps
        {
            File = props.DockerfilePath
        });
        var securityGroup = new SecurityGroup(this, "LambdaSg", new SecurityGroupProps
        {
            Vpc = props.Vpc,
            AllowAllOutbound = true,
            Description = props.SecurityGroupDescription
        });
        Function = new DockerImageFunction(this, "Lambda", new DockerImageFunctionProps
        {
            FunctionName = props.FunctionName,
            Code = imageCode,
            Timeout = Duration.Seconds(30),
            Environment = props.Environment.WithOtel(props.FunctionName),
            Tracing = Tracing.ACTIVE,
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            },
            SecurityGroups = [securityGroup]
        });
        props.Api.AddRoutes(new AddRoutesOptions
        {
            Path = props.RoutePath,
            Integration = new HttpLambdaIntegration($"{props.FunctionName}Integration", Function),
            Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY]
        });
        props.DbProxyAccess.DbProxy.GrantConnect(Function, props.DbConnection.DbUsername);
        var handlerSgRule = new ConnectionRule
        {
            Description = "Allow Lambda to access RDS Proxy",
            Other = props.DbProxyAccess.DbProxySecurityGroup,
            PortRange = Port.Tcp(props.DbConnection.DbPort)
        };
        securityGroup.Connections.AllowTo(handlerSgRule);
    }
}
