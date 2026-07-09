namespace Deployment.Database;

internal sealed record DatabaseConnectionProps
{
    internal required string DbName { get; init; }
    internal required int DbPort { get; init; }
    internal required string DbUsername { get; init; }
}
