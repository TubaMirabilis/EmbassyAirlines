namespace Deployment;

internal static class ExampleDeployment
{
    public static async Task DeployAsync()
    {
        var project = "Example.Api.Lambda";
        Console.WriteLine($"Starting deployment of {project}...");
        var repoUri = await ImageService.EnsureRepositoryAsync("embassy-web");
        var tag = "latest";
        await ImageService.BuildAsync("Example.Api.Lambda.dockerfile", "tubamirabilis/example", tag);
        var fullImageTag = $"{repoUri}:{tag}";
        await ImageService.TagAsync("tubamirabilis/example:latest", fullImageTag);
        await ImageService.PushAsync(fullImageTag);
        var role = await IdentityService.EnsureRoleAsync("ExampleApiLambdaRole");
        await IdentityService.AttachPolicyAsync("arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole", role.RoleName);
        await LambdaService.UpsertFunctionFromImageAsync("ExampleApiLambda", fullImageTag, role.Arn);
        var functionUrl = await LambdaService.CreateFunctionUrlAsync("ExampleApiLambda");
        await LambdaService.AllowPublicInvokeUrlAsync("ExampleApiLambda");
        Console.WriteLine($"Deployment of {project} completed. Function URL: {functionUrl}");
    }
}
