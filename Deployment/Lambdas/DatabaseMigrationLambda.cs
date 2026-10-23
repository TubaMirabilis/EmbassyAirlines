using Amazon.CDK;
using Amazon.CDK.AWS.Ecr.Assets;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.CustomResources;
using Constructs;

namespace Deployment.Lambdas;

internal sealed class DatabaseMigrationLambda : Construct
{
    internal CustomResource Migration { get; }
    internal DatabaseMigrationLambda(
        Construct scope,
        string id,
        DatabaseBackedLambdaProps props) : base(scope, id)
    {
        var imageAsset = new DockerImageAsset(
            this, "Image", new DockerImageAssetProps
            {
                Directory = ".",
                File = props.DockerfilePath
            });
        var imageCode = DockerImageCode.FromEcr(
            imageAsset.Repository, new EcrImageCodeProps
            {
                TagOrDigest = imageAsset.ImageTag
            });
        var lambda = new DatabaseBackedDockerLambda(
            this,
            "Lambda",
            props,
            Duration.Minutes(10),
            imageCode);
        var provider = new Provider(this, "Provider",
            new ProviderProps
            {
                OnEventHandler = lambda.Function
            });
        Migration = new CustomResource(
            this, "Migration", new CustomResourceProps
            {
                ServiceToken = provider.ServiceToken,
                Properties = new Dictionary<string, object>
                {
                    ["MigrationImageHash"] = imageAsset.AssetHash
                }
            });
        Migration.Node.AddDependency(lambda);
        Migration.Node.AddDependency(
            props.DbProxyAccess.DbProxy);
    }
}
