using Amazon.CDK;
using Amazon.CDK.AWS.Apigatewayv2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AwsApigatewayv2Integrations;
using Constructs;

namespace Deployment.Lambdas;

internal sealed class HttpDockerLambda : Construct
{
    internal DockerImageFunction Function { get; }
    internal HttpDockerLambda(
        Construct scope,
        string id,
        HttpDockerLambdaProps props) : base(scope, id)
    {
        var lambda = new DatabaseBackedDockerLambda(
            this,
            "Lambda",
            props,
            Duration.Seconds(30));
        Function = lambda.Function;
        var integration = new HttpLambdaIntegration(
            $"{props.FunctionName}Integration",
            Function);
        foreach (var path in new[]
        {
            props.RoutePath,
            $"{props.RoutePath}/{{proxy+}}"
        })
        {
            props.Api.AddRoutes(new AddRoutesOptions
            {
                Path = path,
                Integration = integration,
                Methods = [Amazon.CDK.AWS.Apigatewayv2.HttpMethod.ANY]
            });
        }
    }
}
