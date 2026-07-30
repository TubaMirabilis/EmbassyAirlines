using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.CustomResources;
using Constructs;
using Deployment.Networking;

namespace Deployment.Lambdas;

internal sealed class DatabaseMigrationLambda : Construct
{
    internal CustomResource Migration { get; }
    internal DatabaseMigrationLambda(Construct scope, string id, DatabaseMigrationLambdaProps props) : base(scope, id)
    {
        var securityGroup = new SecurityGroup(this, "SecurityGroup", new SecurityGroupProps
        {
            Vpc = props.Vpc,
            AllowAllOutbound = true,
            Description = props.SecurityGroupDescription
        });
        securityGroup.Connections.AllowTo(new SecurityGroupConnection
        {
            Description = "Allow migrations Lambda to access RDS Proxy",
            Other = props.DbProxyAccess.DbProxySecurityGroup,
            PortRange = Port.Tcp(props.DbConnection.DbPort)
        });
        var imageAsset = new DockerImageAsset(this, "Image", new DockerImageAssetProps
        {
            Directory = ".",
            File = props.DockerfilePath
        });
        var function = new DockerImageFunction(this, "Function", new DockerImageFunctionProps
        {
            FunctionName = props.FunctionName,
            Code = DockerImageCode.FromEcr(imageAsset.Repository, new EcrImageCodeProps
            {
                TagOrDigest = imageAsset.ImageTag
            }),
            Environment = props.Environment,
            Timeout = Duration.Minutes(10),
            MemorySize = 1536,
            Tracing = Tracing.ACTIVE,
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            },
            SecurityGroups = [securityGroup]
        });
        props.DbProxyAccess.DbProxy.GrantConnect(function, props.DbConnection.DbUsername);
        var provider = new Provider(this, "Provider", new ProviderProps
        {
            OnEventHandler = function
        });
        Migration = new CustomResource(this, "Migration", new CustomResourceProps
        {
            ServiceToken = provider.ServiceToken,
            Properties = new Dictionary<string, object>
            {
                ["MigrationImageHash"] = imageAsset.AssetHash
            }
        });
        Migration.Node.AddDependency(function);
        Migration.Node.AddDependency(props.DbProxyAccess.DbProxy);
    }
}
