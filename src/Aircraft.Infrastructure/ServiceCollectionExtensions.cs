using System.Globalization;
using Aircraft.Infrastructure.Database;
using Amazon.RDS.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shared;

namespace Aircraft.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
    {
        var host = config["DbConnection:Host"];
        var dbName = config["DbConnection:Database"];
        var username = config["DbConnection:Username"];
        var portStr = config["DbConnection:Port"];
        Ensure.NotNullOrEmpty(host);
        Ensure.NotNullOrEmpty(dbName);
        Ensure.NotNullOrEmpty(username);
        Ensure.NotNullOrEmpty(portStr);
        var port = int.Parse(portStr, CultureInfo.InvariantCulture);
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Database = dbName,
            Host = host,
            Port = port,
            SslMode = SslMode.Require,
            Username = username
        }.ConnectionString;
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UsePasswordProvider(
            passwordProvider: _ => throw new NotSupportedException("Use OpenAsync"),
            passwordProviderAsync: async (builder, ct) => await RDSAuthTokenGenerator.GenerateAuthTokenAsync(host, port, username));
        var dataSource = dataSourceBuilder.Build();
        services.AddSingleton(dataSource);
        services.AddDbContext<ApplicationDbContext>((sp, options) => options.UseNpgsql(new NpgsqlConnection(connectionString), x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "aircraft");
            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        })
        .UseSnakeCaseNamingConvention());
        return services;
    }
}
