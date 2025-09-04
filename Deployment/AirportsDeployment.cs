namespace Deployment;

internal static class AirportsDeployment
{
    public static async Task DeployAsync()
    {
        var project = "Airports.Api.Lambda";
        Console.WriteLine($"Starting deployment of {project}...");
        var env = new Dictionary<string, string>
        {
            { "AIRPORTS_MassTransit__Scope", "embassy-airlines" },
            { "AIRPORTS_DynamoDb__TableName", "airports" }
        };
        var repoUri = await ImageService.EnsureRepositoryAsync("airports");
        var tag = "latest";
        await ImageService.BuildAsync("Airports.Api.Lambda.dockerfile", "tubamirabilis/airports", tag);
        var fullImageTag = $"{repoUri}:{tag}";
        await ImageService.TagAsync("tubamirabilis/airports:latest", fullImageTag);
        await ImageService.PushAsync(fullImageTag);
        var role = await IdentityService.EnsureRoleAsync("AirportsApiLambdaRole");
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole", role.RoleName);
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/AmazonDynamoDBFullAccess", role.RoleName);
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/AmazonSQSFullAccess", role.RoleName);
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/AmazonSNSFullAccess", role.RoleName);
        await DynamoDbService.CreateTableIfNotExistsAsync("airports");
        await LambdaService.UpsertFunctionFromImageAsync("AirportsApiLambda", fullImageTag, role.Arn, env);
        var functionUrl = await LambdaService.CreateFunctionUrlAsync("AirportsApiLambda");
        await LambdaService.AllowPublicInvokeUrlAsync("AirportsApiLambda");
        Console.WriteLine($"Deployment of {project} completed. Function URL: {functionUrl}");
    }
}
