using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;

namespace Deployment;

internal sealed class EmbassyAirlinesStack : Stack
{
    internal EmbassyAirlinesStack(Construct scope, string id, IStackProps props) : base(scope, id, props)
    {
        var airportsTable = new Table(this, "AirportsTable", new TableProps
        {
            TableName = "airports",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "Id",
                Type = AttributeType.STRING
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
        var airportCreatedTopic = new Topic(this, "AirportCreatedTopic", new TopicProps
        {
            TopicName = "AirportCreatedTopic"
        });
        var airportUpdatedTopic = new Topic(this, "AirportUpdatedTopic", new TopicProps
        {
            TopicName = "AirportUpdatedTopic"
        });
        var imageCode = DockerImageCode.FromImageAsset(directory: ".", new AssetImageCodeProps
        {
            File = "docker/Airports.Api.Lambda.dockerfile"
        });
        var lambda = new DockerImageFunction(this, "AirportsApiLambda", new DockerImageFunctionProps
        {
            FunctionName = "AirportsApiLambda",
            Code = imageCode,
            Timeout = Duration.Seconds(30),
            Environment = new Dictionary<string, string>
            {
                { "AIRPORTS_DynamoDb__TableName", airportsTable.TableName },
                { "AIRPORTS_SNS__AirportCreatedTopicArn", airportCreatedTopic.TopicArn },
                { "AIRPORTS_SNS__AirportUpdatedTopicArn", airportUpdatedTopic.TopicArn }
            }
        });
        airportsTable.GrantReadWriteData(lambda);
        airportCreatedTopic.GrantPublish(lambda);
        airportUpdatedTopic.GrantPublish(lambda);
        var hostedZone = HostedZone.FromHostedZoneAttributes(this, "EmbassyAirlinesHostedZone", new HostedZoneAttributes
        {
            HostedZoneId = "Z0067852TNRCM5YQCWSI",
            ZoneName = "embassyairlines.com"
        });
        var certificate = new Certificate(this, "EmbassyAirlinesCert", new CertificateProps
        {
            DomainName = "embassyairlines.com",
            Validation = CertificateValidation.FromDns(hostedZone)
        });
        certificate.ApplyRemovalPolicy(RemovalPolicy.RETAIN);
        var domainName = new DomainName(this, "EmbassyAirlinesDomainName", new DomainNameProps
        {
            DomainName = "embassyairlines.com",
            Certificate = certificate
        });
        var api = new HttpApi(this, "EmbassyAirlinesApi", new HttpApiProps
        {
            ApiName = "EmbassyAirlinesApi",
            Description = "Embassy Airlines HTTP API",
            DefaultDomainMapping = new DomainMappingOptions
            {
                DomainName = domainName,
                MappingKey = "api"
            }
        });
        api.AddRoutes(new AddRoutesOptions
        {
            Path = "/airports",
            Integration = new HttpLambdaIntegration("AirportsApiIntegration", lambda),
            Methods = new[] { Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY }
        });
        new ARecord(this, "EmbassyAirlinesApiAliasRecord", new ARecordProps
        {
            Zone = hostedZone,
            RecordName = "",
            Target = RecordTarget.FromAlias(new ApiGatewayv2DomainProperties(domainName.RegionalDomainName, domainName.RegionalHostedZoneId))
        });
    }
}
