using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SecretsManager;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;
using InstanceType = Amazon.CDK.AWS.EC2.InstanceType;

namespace Deployment;

internal sealed class AircraftServiceStack : Stack
{
    internal AircraftServiceStack(Construct scope, string id, AircraftServiceStackProps props) : base(scope, id, props)
    {
        var dbPasswordParam = new CfnParameter(this, "DbPassword", new CfnParameterProps
        {
            Type = "String",
            NoEcho = true,
            Description = "Password for the aircraft RDS user"
        });
        const string dbName = "aircraft";
        const string dbUser = "aircraft_app";
        var dbInstance = new DatabaseInstance(this, "AircraftDb", new DatabaseInstanceProps
        {
            Engine = DatabaseInstanceEngine.Postgres(new PostgresInstanceEngineProps
            {
                Version = PostgresEngineVersion.VER_18_1
            }),
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_WITH_EGRESS
            },
            Credentials = Credentials.FromPassword(username: dbUser, password: SecretValue.CfnParameter(dbPasswordParam)),
            DatabaseName = dbName,
            InstanceType = InstanceType.Of(InstanceClass.T4G, InstanceSize.MICRO),
            AllocatedStorage = 20,
            MultiAz = false,
            DeletionProtection = false,
            RemovalPolicy = RemovalPolicy.DESTROY
        });
        var connectionString = $"Server={dbInstance.DbInstanceEndpointAddress};" + $"Port={dbInstance.DbInstanceEndpointPort};" + $"Database={dbName};" + $"User Id={dbUser};" + $"Password={dbPasswordParam.ValueAsString};" + $"Include Error Detail=true";
        var connectionStringSecret = new Secret(this, "AircraftConnectionStringSecret", new SecretProps
        {
            SecretName = $"{props.EnvironmentName}/Aircraft/ConnectionStrings__DefaultConnection",
            SecretStringValue = SecretValue.UnsafePlainText(connectionString)
        });
        var aircraftCreatedTopic = new Topic(this, "AircraftCreatedTopic", new TopicProps
        {
            TopicName = "AircraftCreatedTopic"
        });
        var bucket = new Bucket(this, "AircraftBucket", new Amazon.CDK.AWS.S3.BucketProps
        {
            BucketName = "embassy-airlines-aircraft-bucket",
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true
        });
        var imageCode = DockerImageCode.FromImageAsset(directory: ".", new AssetImageCodeProps
        {
            File = "docker/Aircraft.Api.Lambda.dockerfile"
        });
        var lambda = new DockerImageFunction(this, "AircraftApiLambda", new DockerImageFunctionProps
        {
            FunctionName = "AircraftApiLambda",
            Code = imageCode,
            Timeout = Duration.Seconds(30),
            Environment = new Dictionary<string, string>
            {
                { "AIRCRAFT_S3__BucketName", bucket.BucketName },
                { "AIRCRAFT_SNS__AircraftCreatedTopicArn", aircraftCreatedTopic.TopicArn }
            },
            Vpc = props.Vpc
        });
        props.Api.AddRoutes(new AddRoutesOptions
        {
            Path = "/aircraft",
            Integration = new HttpLambdaIntegration("AircraftApiIntegration", lambda),
            Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY]
        });
        aircraftCreatedTopic.GrantPublish(lambda);
        bucket.GrantRead(lambda);
        connectionStringSecret.GrantRead(lambda);
        dbInstance.Connections.AllowFrom(lambda, Port.Tcp(5432), "Allow Lambda to access RDS");
    }
}
