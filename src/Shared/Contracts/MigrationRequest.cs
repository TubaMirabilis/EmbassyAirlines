namespace Shared.Contracts;

public sealed record MigrationRequest(string? RequestType, Dictionary<string, object>? ResourceProperties);
