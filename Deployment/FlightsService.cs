using System.Globalization;
using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;

namespace Deployment;

internal sealed class FlightsService : Construct
{
    internal FlightsService(Construct scope, string id, FlightsServiceProps props) : base(scope, id)
    {
        var imageCode = DockerImageCode.FromImageAsset(directory: ".", new AssetImageCodeProps
        {
            File = "docker/Flights.Api.Lambda.dockerfile"
        });
        var lambdaSg = new SecurityGroup(this, "FlightsLambdaSG", new SecurityGroupProps
        {
            Vpc = props.Vpc,
            AllowAllOutbound = true,
            Description = "Security group for Flights API Lambda"
        });
        var apiLambda = new DockerImageFunction(this, "FlightsApiLambda", new DockerImageFunctionProps
        {
            FunctionName = "FlightsApiLambda",
            Code = imageCode,
            Timeout = Duration.Seconds(30),
            Environment = new Dictionary<string, string>
            {
                { "FLIGHTS_DbConnection__Database", props.DbName },
                { "FLIGHTS_DbConnection__Host", props.DbProxy.Endpoint },
                { "FLIGHTS_DbConnection__Port", props.DbPort.ToString(CultureInfo.InvariantCulture) },
                { "FLIGHTS_DbConnection__Username", props.DbUsername },
                { "FLIGHTS_SNS__FlightScheduledTopicArn", props.FlightScheduledTopic.TopicArn }
            },
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            },
            SecurityGroups = [lambdaSg]
        });
        props.Api.AddRoutes(new AddRoutesOptions
        {
            Path = "/flights",
            Integration = new HttpLambdaIntegration("FlightsApiIntegration", apiLambda),
            Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY]
        });
        props.DbProxy.GrantConnect(apiLambda, props.DbUsername);
        lambdaSg.Connections.AllowTo(props.DbProxySecurityGroup, Port.Tcp(props.DbPort), "Allow Lambda to access RDS Proxy");
        props.FlightScheduledTopic.GrantPublish(apiLambda);
        var handlerSg = new SecurityGroup(this, "FlightsAircraftCreatedHandlerSG", new SecurityGroupProps
        {
            Vpc = props.Vpc,
            AllowAllOutbound = true,
            Description = "Security group for Flights AircraftCreated Event Handler Lambda"
        });
        handlerSg.Connections.AllowTo(props.DbProxySecurityGroup, Port.Tcp(props.DbPort), "Allow handler Lambda to access RDS Proxy");
        var handlerCode = Code.FromAsset("src/Flights.Api.Lambda.MessageHandlers.AircraftCreated", new Amazon.CDK.AWS.S3.Assets.AssetOptions
        {
            Bundling = new BundlingOptions
            {
                Image = DockerImage.FromRegistry("mcr.microsoft.com/dotnet/sdk:10.0"),
                Command = ["bash", "-lc", "dotnet publish -c Release -o /asset-output"]
            }
        });
        var aircraftCreatedHandler = new Function(this, "FlightsAircraftCreatedHandler", new FunctionProps
        {
            FunctionName = "FlightsAircraftCreatedHandler",
            Runtime = Runtime.DOTNET_10,
            Handler = "Flights.Api.Lambda.MessageHandlers.AircraftCreated::Flights.Api.Lambda.MessageHandlers.AircraftCreated.Function::FunctionHandler",
            Code = handlerCode,
            Timeout = Duration.Seconds(30),
            MemorySize = 512,
            Environment = new Dictionary<string, string>
            {
                { "FLIGHTS_DbConnection__Database", props.DbName },
                { "FLIGHTS_DbConnection__Host", props.DbProxy.Endpoint },
                { "FLIGHTS_DbConnection__Port", props.DbPort.ToString(CultureInfo.InvariantCulture) },
                { "FLIGHTS_DbConnection__Username", props.DbUsername }
            },
            Vpc = props.Vpc,
            VpcSubnets = new SubnetSelection
            {
                SubnetType = SubnetType.PRIVATE_ISOLATED
            },
            SecurityGroups = [handlerSg]
        });
        props.DbProxy.GrantConnect(aircraftCreatedHandler, props.DbUsername);
        var aircraftCreatedDlq = new Queue(this, "FlightsAircraftCreatedDLQ");
        var aircraftCreatedQueue = new Queue(this, "FlightsAircraftCreatedQueue", new QueueProps
        {
            DeadLetterQueue = new DeadLetterQueue
            {
                MaxReceiveCount = 3,
                Queue = aircraftCreatedDlq
            }
        });
        props.AircraftCreatedTopic.AddSubscription(new SqsSubscription(aircraftCreatedQueue));
        aircraftCreatedHandler.AddEventSource(new SqsEventSource(aircraftCreatedQueue, new SqsEventSourceProps
        {
            BatchSize = 10,
            ReportBatchItemFailures = true
        }));
        aircraftCreatedQueue.GrantConsumeMessages(aircraftCreatedHandler);
    }
}
