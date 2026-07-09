using System.Globalization;
using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Constructs;
using Deployment.Lambdas;

namespace Deployment.Services;

internal sealed class AircraftService : Construct
{
    internal AircraftService(Construct scope, string id, AircraftServiceProps props) : base(scope, id)
    {
        var commonEnv = new Dictionary<string, string>
        {
            { "AIRCRAFT_DbConnection__Database", props.DbConnection.DbName },
            { "AIRCRAFT_DbConnection__Host", props.DbProxyAccess.DbProxy.Endpoint },
            { "AIRCRAFT_DbConnection__Port", props.DbConnection.DbPort.ToString(CultureInfo.InvariantCulture) },
            { "AIRCRAFT_DbConnection__Username", props.DbConnection.DbUsername }
        };
        var bucket = new Bucket(this, "AircraftBucket", new BucketProps
        {
            BucketName = $"aircraft-bucket-{Aws.ACCOUNT_ID}-{Aws.REGION}",
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true
        });
        var api = new HttpDockerLambda(this, "AircraftApi", new HttpDockerLambdaProps
        {
            Api = props.Api,
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            DockerfilePath = "docker/Aircraft.Api.Lambda.dockerfile",
            Environment = new Dictionary<string, string>(commonEnv)
            {
                { "AIRCRAFT_S3__BucketName", bucket.BucketName }
            },
            FunctionName = "AircraftApiLambda",
            RoutePath = "/aircraft",
            SecurityGroupDescription = "Security group for Aircraft API Lambda",
            Vpc = props.Vpc
        });
        var lambda = api.Function;
        bucket.GrantRead(lambda);
        new EventHandlerLambda(this, "AircraftFlightArrivedHandlerLambda", new EventHandlerLambdaProps
        {
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "AircraftFlightArrivedHandlerLambda",
            Path = "docker/Aircraft.Api.Lambda.MessageHandlers.FlightArrived.dockerfile",
            SecurityGroupDescription = "Security group for Aircraft FlightArrived handler Lambda",
            Topic = props.FlightArrivedTopic,
            Vpc = props.Vpc
        });
        new EventHandlerLambda(this, "AircraftFlightMarkedAsDelayedEnRouteHandlerLambda", new EventHandlerLambdaProps
        {
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "AircraftFlightMarkedAsDelayedEnRouteHandlerLambda",
            Path = "docker/Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsDelayedEnRoute.dockerfile",
            SecurityGroupDescription = "Security group for Aircraft FlightMarkedAsDelayedEnRoute handler Lambda",
            Topic = props.FlightMarkedAsDelayedEnRouteTopic,
            Vpc = props.Vpc
        });
        new EventHandlerLambda(this, "AircraftFlightMarkedAsEnRouteHandlerLambda", new EventHandlerLambdaProps
        {
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            Environment = new Dictionary<string, string>(commonEnv),
            FunctionName = "AircraftFlightMarkedAsEnRouteHandlerLambda",
            Path = "docker/Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsEnRoute.dockerfile",
            SecurityGroupDescription = "Security group for Aircraft FlightMarkedAsEnRoute handler Lambda",
            Topic = props.FlightMarkedAsEnRouteTopic,
            Vpc = props.Vpc
        });
        new PublisherLambda(this, "AircraftPublisherLambda", new PublisherLambdaProps
        {
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            DockerfilePath = "docker/Aircraft.Publisher.Lambda.dockerfile",
            Environment = new Dictionary<string, string>(commonEnv)
            {
                { "AIRCRAFT_SNS__AircraftCreatedTopicArn", props.AircraftCreatedTopic.TopicArn }
            },
            FunctionName = "AircraftPublisherLambda",
            PollInterval = Duration.Minutes(1),
            SecurityGroupDescription = "Security group for Aircraft outbox publisher Lambda",
            Topics = [props.AircraftCreatedTopic],
            Vpc = props.Vpc
        });
    }
}
