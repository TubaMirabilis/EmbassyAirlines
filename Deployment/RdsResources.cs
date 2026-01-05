using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Constructs;
using InstanceType = Amazon.CDK.AWS.EC2.InstanceType;

namespace Deployment;

internal sealed class RdsResources : Construct
{
    internal RdsResources(Construct scope, string id, RdsResourcesProps props) : base(scope, id)
    {
        DbName = props.DbName;
        DbPort = props.DbPort;
        DbInstance = new DatabaseInstance(this, "EmbassyAirlinesDb", new DatabaseInstanceProps
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
            Credentials = Credentials.FromGeneratedSecret(props.DbUsername),
            InstanceType = InstanceType.Of(InstanceClass.T4G, InstanceSize.MICRO),
            DatabaseName = DbName,
            Port = DbPort,
            AllocatedStorage = 20,
            MultiAz = false,
            DeletionProtection = false,
            RemovalPolicy = RemovalPolicy.DESTROY
        });
        DbProxySecurityGroup = new SecurityGroup(this, "EmbassyAirlinesDbProxySecurityGroup", new SecurityGroupProps
        {
            Vpc = props.Vpc,
            Description = "Security group for RDS Proxy for Embassy Airlines DB",
            AllowAllOutbound = true
        });
        DbInstance.Connections.AllowDefaultPortFrom(DbProxySecurityGroup, "RDS Proxy to DB");
        ArgumentNullException.ThrowIfNull(DbInstance.Secret);
        DbProxy = new DatabaseProxy(this, "EmbassyAirlinesDbProxy", new DatabaseProxyProps
        {
            IamAuth = true,
            ProxyTarget = ProxyTarget.FromInstance(DbInstance),
            Secrets = [DbInstance.Secret],
            SecurityGroups = [DbProxySecurityGroup],
            Vpc = props.Vpc
        });
    }
    internal DatabaseInstance DbInstance { get; }
    internal string DbName { get; }
    internal int DbPort { get; }
    internal DatabaseProxy DbProxy { get; }
    internal SecurityGroup DbProxySecurityGroup { get; }
}
