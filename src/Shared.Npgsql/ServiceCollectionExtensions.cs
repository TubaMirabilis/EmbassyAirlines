using System.Globalization;
using Amazon.RDS.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Shared.Npgsql;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseConnection<TDbContext>(this IServiceCollection services, IConfiguration config, bool useNodaTime, string schema) where TDbContext : DbContext
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
        if (useNodaTime)
        {
            dataSourceBuilder.UseNodaTime();
        }
        var dataSource = dataSourceBuilder.Build();
        services.AddSingleton(dataSource);
        services.AddSingleton<InsertOutboxMessagesInterceptor>();
        services.AddDbContext<TDbContext>((sp, options) => options.UseNpgsql(dataSource, x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", schema);
            if (useNodaTime)
            {
                x.UseNodaTime();
            }
            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        })
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));
        return services;
    }
}
