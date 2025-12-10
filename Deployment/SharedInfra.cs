using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using Constructs;

namespace Deployment;

internal sealed class SharedInfra : Construct
{
    internal SharedInfra(Construct scope, string id) : base(scope, id)
    {
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
        Api = new HttpApi(this, "EmbassyAirlinesApi", new HttpApiProps
        {
            ApiName = "EmbassyAirlinesApi",
            Description = "Embassy Airlines HTTP API",
            DefaultDomainMapping = new DomainMappingOptions
            {
                DomainName = domainName,
                MappingKey = "api"
            }
        });
        new ARecord(this, "EmbassyAirlinesApiAliasRecord", new ARecordProps
        {
            Zone = hostedZone,
            RecordName = "",
            Target = RecordTarget.FromAlias(new ApiGatewayv2DomainProperties(domainName.RegionalDomainName, domainName.RegionalHostedZoneId))
        });
    }
    internal HttpApi Api { get; }
}
