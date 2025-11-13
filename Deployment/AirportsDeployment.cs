namespace Deployment;

internal static class AirportsDeployment
{
    public static async Task DeployAsync()
    {
        var project = "Airports.Api.Lambda";
        Console.WriteLine($"Starting deployment of {project}...");
        var repoUri = await ImageService.EnsureRepositoryAsync("airports");
        var tag = "latest";
        await ImageService.BuildAsync("Airports.Api.Lambda.dockerfile", "tubamirabilis/airports", tag);
        var fullImageTag = $"{repoUri}:{tag}";
        await ImageService.TagAsync("tubamirabilis/airports:latest", fullImageTag);
        await DynamoDbService.CreateTableIfNotExistsAsync("airports");
        var hostPort = 9000;
        var awsAccessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID") ?? throw new InvalidOperationException("AWS_ACCESS_KEY_ID environment variable is not set.");
        var awsSecretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY") ?? throw new InvalidOperationException("AWS_SECRET_ACCESS_KEY environment variable is not set.");
        var testEnv = new Dictionary<string, string>
        {
            { "AIRPORTS_MassTransit__Scope", "embassy-airlines" },
            { "AIRPORTS_DynamoDb__TableName", "airports" },
            { "AWS_ACCESS_KEY_ID", awsAccessKeyId },
            { "AWS_SECRET_ACCESS_KEY", awsSecretAccessKey }
        };
        await ImageService.RunAsync(fullImageTag, hostPort, 8080, "airports-service", testEnv);
        var body = """
        {
            "version": "2.0",
            "routeKey": "GET /airports",
            "rawPath": "/airports",
            "rawQueryString": "",
            "headers": {
                "accept": "application/json",
                "host": "localhost",
                "user-agent": "curl/7.79.1"
            },
            "requestContext": {
                "http": {
                "method": "GET",
                "path": "/airports",
                "protocol": "HTTP/1.1",
                "sourceIp": "127.0.0.1",
                "userAgent": "curl/7.79.1"
                }
            },
            "isBase64Encoded": false
        }
        """;
        var url = $"http://localhost:{hostPort}/2015-03-31/functions/function/invocations";
        var success = await ImageService.SmokeTestPostAsync(url, body);
        if (!success)
        {
            await ImageService.StopAndRemoveContainerAsync("airports-service");
            return;
        }
        await ImageService.StopAndRemoveContainerAsync("airports-service");
        await ImageService.PushAsync(fullImageTag);
        var role = await IdentityService.EnsureRoleAsync("AirportsApiLambdaRole");
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole", role.RoleName);
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/AmazonDynamoDBFullAccess", role.RoleName);
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/AmazonSQSFullAccess", role.RoleName);
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/AmazonSNSFullAccess", role.RoleName);
        var env = new Dictionary<string, string>
        {
            { "AIRPORTS_MassTransit__Scope", "embassy-airlines" },
            { "AIRPORTS_DynamoDb__TableName", "airports" }
        };
        await LambdaService.UpsertFunctionFromImageAsync("AirportsApiLambda", fullImageTag, role.Arn, env);
        var functionUrl = await LambdaService.CreateFunctionUrlAsync("AirportsApiLambda");
        await LambdaService.AllowPublicInvokeUrlAsync("AirportsApiLambda");
        Console.WriteLine($"Deployment of {project} completed. Function URL: {functionUrl}");
    }
}
