using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Constructs;
using InstanceType = Amazon.CDK.AWS.EC2.InstanceType;

namespace Deployment;

internal sealed class DatabaseStack : Stack
{
    internal DatabaseStack(Construct scope, string id, DatabaseStackProps props) : base(scope, id, props) => DbInstance = new DatabaseInstance(this, "EmbassyAirlinesDb", new DatabaseInstanceProps
    {
        Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps
        {
            Version = PostgresEngineVersion.VER_18_1
        }),
        Vpc = props.Vpc,
        VpcSubnets = new SubnetSelection
        {
            SubnetType = SubnetType.PRIVATE_ISOLATED
        },
        Credentials = Credentials.FromGeneratedSecret("admin"),
        InstanceType = InstanceType.Of(InstanceClass.T4G, InstanceSize.MICRO),
        AllocatedStorage = 20,
        MultiAz = false,
        DeletionProtection = false,
        RemovalPolicy = RemovalPolicy.DESTROY
    });
    internal DatabaseInstance DbInstance { get; }
}
