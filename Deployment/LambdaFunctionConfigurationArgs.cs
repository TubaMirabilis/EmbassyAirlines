namespace Deployment;

internal sealed record LambdaFunctionConfigurationArgs
{
    public required Dictionary<string, string> Environment { get; init; }
    public required string FunctionName { get; init; }
    public required string ImageUri { get; init; }
    public required string RoleArn { get; init; }
}
