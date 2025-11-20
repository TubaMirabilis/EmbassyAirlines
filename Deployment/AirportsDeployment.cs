namespace Deployment;

internal static class AirportsDeployment
{
    public static async Task DeployAsync()
    {
        var awsAccessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        if (string.IsNullOrWhiteSpace(awsAccessKeyId))
        {
            throw new InvalidOperationException("AWS_ACCESS_KEY_ID environment variable is not set.");
        }
        var awsSecretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        if (string.IsNullOrWhiteSpace(awsSecretAccessKey))
        {
            throw new InvalidOperationException("AWS_SECRET_ACCESS_KEY environment variable is not set.");
        }
        var project = "Airports.Api.Lambda";
        Console.WriteLine($"Starting deployment of {project}...");
        var repoUri = await ImageService.EnsureRepositoryAsync("airports");
        var tag = "latest";
        await ImageService.BuildAsync("Airports.Api.Lambda.dockerfile", "tubamirabilis/airports", tag);
        var fullImageTag = $"{repoUri}:{tag}";
        await ImageService.TagAsync("tubamirabilis/airports:latest", fullImageTag);
        await DynamoDbService.CreateTableIfNotExistsAsync("airports-test");
        var hostPort = 9000;
        var testAirportCreatedTopicArn = await SnsService.EnsureTopicAsync("AirportCreatedTopicForTesting");
        var testAirportUpdatedTopicArn = await SnsService.EnsureTopicAsync("AirportUpdatedTopicForTesting");
        var testEnv = new Dictionary<string, string>
        {
            { "AIRPORTS_DynamoDb__TableName", "airports-test" },
            { "AIRPORTS_SNS__AirportCreatedTopicArn", testAirportCreatedTopicArn },
            { "AIRPORTS_SNS__AirportUpdatedTopicArn", testAirportUpdatedTopicArn },
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
        var success = await SmokeTester.TestLambdaProxyAsync(url, body);
        if (!success)
        {
            await ImageService.StopAndRemoveContainerAsync("airports-service");
            throw new InvalidOperationException("Smoke test failed. Deployment aborted.");
        }
        await ImageService.StopAndRemoveContainerAsync("airports-service");
        await ImageService.PushAsync(fullImageTag);
        var role = await IdentityService.EnsureRoleAsync("AirportsApiLambdaRole");
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole", role.RoleName);
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/AmazonDynamoDBFullAccess", role.RoleName);
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/AmazonSNSFullAccess", role.RoleName);
        await DynamoDbService.CreateTableIfNotExistsAsync("airports");
        var airportCreatedTopicArn = await SnsService.EnsureTopicAsync("AirportCreatedTopic");
        var airportUpdatedTopicArn = await SnsService.EnsureTopicAsync("AirportUpdatedTopic");
        var env = new Dictionary<string, string>
        {
            { "AIRPORTS_DynamoDb__TableName", "airports" },
            { "AIRPORTS_SNS__AirportCreatedTopicArn", airportCreatedTopicArn },
            { "AIRPORTS_SNS__AirportUpdatedTopicArn", airportUpdatedTopicArn }
        };
        var args = new LambdaFunctionConfigurationArgs
        {
            FunctionName = "AirportsApiLambda",
            ImageUri = fullImageTag,
            RoleArn = role.Arn,
            Environment = env
        };
        await LambdaService.UpsertFunctionFromImageAsync(args);
        var functionUrl = await LambdaService.CreateFunctionUrlAsync("AirportsApiLambda");
        await LambdaService.AllowPublicInvokeUrlAsync("AirportsApiLambda");
        Console.WriteLine($"Deployment of {project} completed. Function URL: {functionUrl}");
    }
}
