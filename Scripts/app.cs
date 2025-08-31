#:package AWSSDK.ECR@4.0.3.6
#:package AWSSDK.IdentityManagement@4.0.3.2
#:package AWSSDK.Lambda@4.0.2.8
#:package Ductus.FluentDocker@2.85.0
using System.Diagnostics;
using System.IO.Pipelines;
using Amazon.ECR;
using Amazon.ECR.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Ductus.FluentDocker.Builders;

var uri = await CreateRepositoryAsync("embassy-web");
await BuildDockerImageAsync();
var localImageTag = "tubamirabilis/example:latest";
var remoteImageUri = $"{uri}:latest";
RunDockerCommand($"tag {localImageTag} {remoteImageUri}");
Console.WriteLine($"Pushing image {remoteImageUri} to ECR...");
RunDockerCommand($"push {remoteImageUri}");
Console.WriteLine("Image push completed.");
var role = await CreateRoleAsync();
Console.WriteLine($"Using role: {role.Arn}");
await AttachPolicyToRoleAsync("arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole", role.RoleName);
var functionArn = await CreateFunctionAsync(remoteImageUri, role.Arn);
Console.WriteLine($"Using function ARN: {functionArn}");
var functionUrl = await CreateFunctionUrlAsync();
Console.WriteLine($"Using function URL: {functionUrl}");
await AllowPublicInvokeUrlAsync();

static async Task<string> AttachPolicyToRoleAsync(string policyArn, string roleName)
{
    using var iamClient = new AmazonIdentityManagementServiceClient();
    var req = new AttachRolePolicyRequest
    {
        PolicyArn = policyArn,
        RoleName = roleName
    };
    await iamClient.AttachRolePolicyAsync(req);
    return roleName;
}
static async Task<string> CreateFunctionUrlAsync()
{
    using var lambdaClient = new AmazonLambdaClient();
    var req = new ListFunctionUrlConfigsRequest
    {
        FunctionName = "ExampleApiLambda"
    };
    var res = await lambdaClient.ListFunctionUrlConfigsAsync(req);
    if (res.FunctionUrlConfigs.Count > 0)
    {
        Console.WriteLine("Function URL already exists.");
        return res.FunctionUrlConfigs[0].FunctionUrl;
    }
    var req2 = new CreateFunctionUrlConfigRequest
    {
        AuthType = FunctionUrlAuthType.NONE,
        FunctionName = "ExampleApiLambda"
    };
    var res2 = await lambdaClient.CreateFunctionUrlConfigAsync(req2);
    return res2.FunctionUrl;
}
static async Task<string> CreateFunctionAsync(string remoteImageUri, string role)
{
    using var lambdaClient = new AmazonLambdaClient();
    var req = new ListFunctionsRequest();
    var res = await lambdaClient.ListFunctionsAsync(req);
    var existingFunction = res.Functions.Find(f => f.FunctionName == "ExampleApiLambda");
    if (existingFunction is not null)
    {
        Console.WriteLine($"Function {existingFunction.FunctionName} already exists. Updating function code...");
        var updateReq = new UpdateFunctionCodeRequest
        {
            FunctionName = existingFunction.FunctionName,
            ImageUri = remoteImageUri,
            Publish = true
        };
        await lambdaClient.UpdateFunctionCodeAsync(updateReq);
        return existingFunction.FunctionArn;
    }
    var req2 = new CreateFunctionRequest
    {
        Code = new FunctionCode
        {
            ImageUri = remoteImageUri
        },
        FunctionName = "ExampleApiLambda",
        PackageType = PackageType.Image,
        Publish = true,
        Role = role
    };
    var res2 = await lambdaClient.CreateFunctionAsync(req2);
    return res2.FunctionArn;
}
static async Task<Role> CreateRoleAsync()
{
    using var iamClient = new AmazonIdentityManagementServiceClient();
    var req = new ListRolesRequest();
    var res = await iamClient.ListRolesAsync(req);
    var existingRole = res.Roles.Find(r => r.RoleName == "ExampleApiLambdaRole");
    if (existingRole is not null)
    {
        Console.WriteLine($"Role {existingRole.RoleName} already exists.");
        return existingRole;
    }
    var req2 = new CreateRoleRequest
    {
        RoleName = "ExampleApiLambdaRole",
        AssumeRolePolicyDocument = @"{
            ""Version"": ""2012-10-17"",
            ""Statement"": [
                {
                    ""Effect"": ""Allow"",
                    ""Principal"": {
                        ""Service"": ""lambda.amazonaws.com""
                    },
                    ""Action"": ""sts:AssumeRole""
                }
            ]
        }"
    };
    var res2 = await iamClient.CreateRoleAsync(req2);
    return res2.Role;
}
static async Task<string> CreateRepositoryAsync(string name)
{
    using var client = new AmazonECRClient();
    var req = new DescribeRepositoriesRequest();
    var res = await client.DescribeRepositoriesAsync(req);
    var exists = res.Repositories.Exists(r => r.RepositoryName == name);
    if (exists)
    {
        Console.WriteLine($"Repository {name} already exists.");
        return res.Repositories.First(r => r.RepositoryName == name).RepositoryUri;
    }
    var req2 = new CreateRepositoryRequest
    {
        RepositoryName = name
    };
    var res2 = await client.CreateRepositoryAsync(req2);
    Console.WriteLine($"Created repository: {res2.Repository.RepositoryName}");
    return res2.Repository.RepositoryUri;
}

static async Task BuildDockerImageAsync()
{
    Console.WriteLine("Building Docker image...");
    var dir = Directory.GetCurrentDirectory();
    var dockerfilePath = Path.Combine(dir, "docker", "Example.Api.Lambda.dockerfile");
    if (!File.Exists(dockerfilePath))
    {
        throw new FileNotFoundException("Dockerfile not found.", dockerfilePath);
    }
    var dockerfileContents = await File.ReadAllTextAsync(dockerfilePath);
    var image = new Builder().DefineImage("tubamirabilis/example")
                            .ReuseIfAlreadyExists()
                            .FromString(dockerfileContents)
                            .WorkingFolder(dir)
                            .Build();
    Console.WriteLine("Docker image built successfully.");
    Console.WriteLine($"Image ID: {image.Id}");
    var dockerfile = Path.Combine(dir, "Dockerfile");
    if (File.Exists(dockerfile))
    {
        File.Delete(dockerfile);
        Console.WriteLine("Removed Dockerfile.");
    }
}

static void RunDockerCommand(string arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = "docker",
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    using var process = new Process { StartInfo = psi };
    process.OutputDataReceived += (s, e) => { if (e.Data != null) { Console.WriteLine(e.Data); } };
    process.ErrorDataReceived += (s, e) => { if (e.Data != null) { Console.Error.WriteLine(e.Data); } };
    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException($"Docker command failed: docker {arguments}");
    }
}
static async Task AllowPublicInvokeUrlAsync()
{
    using var lambda = new AmazonLambdaClient();
    try
    {
        await lambda.AddPermissionAsync(new AddPermissionRequest
        {
            FunctionName = "ExampleApiLambda",
            StatementId = "AllowPublicInvokeUrl",
            Action = "lambda:InvokeFunctionUrl",
            Principal = "*",
            FunctionUrlAuthType = FunctionUrlAuthType.NONE
        });
        Console.WriteLine("Public invoke permission added.");
    }
    catch (ResourceConflictException)
    {
        Console.WriteLine("Permission already exists.");
    }
}