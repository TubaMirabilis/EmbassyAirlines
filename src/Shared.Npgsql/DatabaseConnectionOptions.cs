namespace Shared.Npgsql;

public sealed record DatabaseConnectionOptions
{
    public required string Host { get; init; }
    public required string Database { get; init; }
    public required string Username { get; init; }
    public required int Port { get; init; }

    public void Validate()
    {
        Ensure.NotNullOrEmpty(Host);
        Ensure.NotNullOrEmpty(Database);
        Ensure.NotNullOrEmpty(Username);
        Ensure.GreaterThanZero(Port);
    }
}
