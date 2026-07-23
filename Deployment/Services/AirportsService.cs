using System.Globalization;
using Amazon.CDK;
using Constructs;
using Deployment.Lambdas;

namespace Deployment.Services;

internal sealed class AirportsService : Construct
{
    internal AirportsService(Construct scope, string id, AirportsServiceProps props) : base(scope, id)
    {
        var commonEnv = new Dictionary<string, string>
        {
            { "AIRPORTS_DbConnection__Database", props.DbConnection.DbName },
            { "AIRPORTS_DbConnection__Host", props.DbProxyAccess.DbProxy.Endpoint },
            { "AIRPORTS_DbConnection__Port", props.DbConnection.DbPort.ToString(CultureInfo.InvariantCulture) },
            { "AIRPORTS_DbConnection__Username", props.DbConnection.DbUsername }
        };
        new HttpDockerLambda(this, "AirportsApi", new HttpDockerLambdaProps
        {
            Api = props.Api,
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            DockerfilePath = "docker/Airports.Api.Lambda.dockerfile",
            Environment = commonEnv,
            FunctionName = "AirportsApiLambda",
            RoutePath = "/airports",
            SecurityGroupDescription = "Security group for Airports API Lambda",
            Vpc = props.Vpc
        });
        new PublisherLambda(this, "AirportsPublisherLambda", new PublisherLambdaProps
        {
            DbConnection = props.DbConnection,
            DbProxyAccess = props.DbProxyAccess,
            DockerfilePath = "docker/Airports.Publisher.Lambda.dockerfile",
            Environment = new Dictionary<string, string>(commonEnv)
            {
                { "AIRPORTS_SNS__AirportCreatedTopicArn", props.AirportCreatedTopic.TopicArn },
                { "AIRPORTS_SNS__AirportUpdatedTopicArn", props.AirportUpdatedTopic.TopicArn }
            },
            FunctionName = "AirportsPublisherLambda",
            PollInterval = Duration.Minutes(1),
            SecurityGroupDescription = "Security group for Airports outbox publisher Lambda",
            Topics =
            [
                props.AirportCreatedTopic,
                props.AirportUpdatedTopic
            ],
            Vpc = props.Vpc
        });
    }
}
