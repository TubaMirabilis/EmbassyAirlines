using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;

namespace Deployment;

internal sealed class AirportsServiceStackProps : StackProps
{
    internal required HttpApi Api { get; init; }
}
