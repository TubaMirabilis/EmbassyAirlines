using Amazon.Lambda;
using Amazon.Lambda.Model;

namespace Deployment;

internal static class LambdaService
{
    public static async Task<string> UpsertFunctionFromImageAsync(string functionName, string imageUri, string roleArn, Dictionary<string, string> environment)
    {
        Console.WriteLine($"Creating or updating Lambda function '{functionName}' with image '{imageUri}'...");
        using var lambdaClient = new AmazonLambdaClient();
        var req = new ListFunctionsRequest();
        var res = await lambdaClient.ListFunctionsAsync(req);
        var existingFunction = res.Functions.Find(f => f.FunctionName == functionName);
        if (existingFunction is not null)
        {
            Console.WriteLine($"Function {existingFunction.FunctionName} already exists. Updating function code...");
            var updateReq = new UpdateFunctionCodeRequest
            {
                FunctionName = existingFunction.FunctionName,
                ImageUri = imageUri,
                Publish = true
            };
            await lambdaClient.UpdateFunctionCodeAsync(updateReq);
            return existingFunction.FunctionArn;
        }
        var req2 = new CreateFunctionRequest
        {
            Code = new FunctionCode
            {
                ImageUri = imageUri
            },
            Environment = new Amazon.Lambda.Model.Environment
            {
                Variables = environment
            },
            FunctionName = functionName,
            PackageType = PackageType.Image,
            Publish = true,
            Role = roleArn,
            Timeout = 30
        };
        var res2 = await lambdaClient.CreateFunctionAsync(req2);
        return res2.FunctionArn;
    }
    public static async Task<string> CreateFunctionUrlAsync(string functionName)
    {
        Console.WriteLine($"Creating Function URL for Lambda function '{functionName}'...");
        using var lambdaClient = new AmazonLambdaClient();
        var req = new ListFunctionUrlConfigsRequest
        {
            FunctionName = functionName
        };
        var res = await lambdaClient.ListFunctionUrlConfigsAsync(req);
        if (res.FunctionUrlConfigs.Count > 0)
        {
            Console.WriteLine($"Function URL for Lambda function '{functionName}' already exists.");
            return res.FunctionUrlConfigs[0].FunctionUrl;
        }
        var req2 = new CreateFunctionUrlConfigRequest
        {
            AuthType = FunctionUrlAuthType.NONE,
            FunctionName = functionName
        };
        var res2 = await lambdaClient.CreateFunctionUrlConfigAsync(req2);
        return res2.FunctionUrl;
    }
    public static async Task AllowPublicInvokeUrlAsync(string functionName)
    {
        Console.WriteLine($"Allowing public invoke URL for Lambda function '{functionName}'...");
        try
        {
            using var lambdaClient = new AmazonLambdaClient();
            var req = new AddPermissionRequest
            {
                FunctionName = functionName,
                StatementId = "AllowPublicInvokeUrl",
                Action = "lambda:InvokeFunctionUrl",
                Principal = "*",
                FunctionUrlAuthType = FunctionUrlAuthType.NONE
            };
            await lambdaClient.AddPermissionAsync(req);
        }
        catch (ResourceConflictException)
        {
            Console.WriteLine($"Permission 'AllowPublicInvokeUrl' already exists for Lambda function '{functionName}'. Skipping...");
        }
    }
}
