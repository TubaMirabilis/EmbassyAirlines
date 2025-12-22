using System.Data.Common;
using Amazon.RDS.Util;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aircraft.Api.Lambda.Database;

internal sealed class EntityFrameworkInterceptor : IDbConnectionInterceptor
{
    private readonly IConfiguration _config;
    private readonly ILogger<EntityFrameworkInterceptor> _logger;
    public EntityFrameworkInterceptor(IConfiguration config, ILogger<EntityFrameworkInterceptor> logger)
    {
        _config = config;
        _logger = logger;
    }
    public InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        var host = _config["DbConnection:Host"];
        var dbName = _config["DbConnection:Database"];
        var user = _config["DbConnection:Username"];
        _logger.LogInformation("Generating auth token for database connection to {Host}:{Database} with user {User}", host, dbName, user);
        var authToken = RDSAuthTokenGenerator.GenerateAuthToken(host, 5432, user);
        var cs = $"Server={host};Database={dbName};User Id={user};Password={authToken};SslMode=Require;TrustServerCertificate=true;";
        connection.ConnectionString = cs;
        return result;
    }
    public ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = new CancellationToken())
    {
        var r = ConnectionOpening(connection, eventData, result);
        return ValueTask.FromResult(r);
    }
    public void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
    }
    public async Task ConnectionOpenedAsync(DbConnection connection,
                                            ConnectionEndEventData eventData,
                                            CancellationToken cancellationToken = new CancellationToken())
    {
    }
    public InterceptionResult ConnectionClosing(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) => result;
    public async ValueTask<InterceptionResult> ConnectionClosingAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) => result;
    public void ConnectionClosed(DbConnection connection, ConnectionEndEventData eventData)
    {
    }
    public async Task ConnectionClosedAsync(DbConnection connection, ConnectionEndEventData eventData)
    {
    }
    public void ConnectionFailed(DbConnection connection, ConnectionErrorEventData eventData)
    {
    }
    public async Task ConnectionFailedAsync(DbConnection connection,
                                            ConnectionErrorEventData eventData,
                                            CancellationToken cancellationToken = new CancellationToken())
    {
    }
}
