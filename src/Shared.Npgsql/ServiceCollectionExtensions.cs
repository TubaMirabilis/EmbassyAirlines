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
        var dbConfig = config.GetSection("DbConnection").Get<DatabaseConnectionOptions>();
        Ensure.NotNull(dbConfig);
        dbConfig.Validate();
        var connectionString = BuildConnectionString(dbConfig);
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        if (useNodaTime)
        {
            dataSourceBuilder.UseNodaTime();
        }
        dataSourceBuilder.UsePasswordProvider(
            passwordProvider: _ => throw new NotSupportedException("Use OpenAsync"),
            passwordProviderAsync: async (builder, ct) => await RDSAuthTokenGenerator.GenerateAuthTokenAsync(dbConfig.Host, dbConfig.Port, dbConfig.Username));
        AddDatabaseConnection<TDbContext>(services, dataSourceBuilder.Build(), useNodaTime, schema);
        return services;
    }
    public static IServiceCollection AddDatabaseConnection<TDbContext>(
    this IServiceCollection services,
    string connectionString,
    bool useNodaTime,
    string schema)
    where TDbContext : DbContext
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        if (useNodaTime)
        {
            builder.UseNodaTime();
        }
        return AddDatabaseConnection<TDbContext>(
            services,
            builder.Build(),
            useNodaTime,
            schema);
    }
    private static IServiceCollection AddDatabaseConnection<TDbContext>(
    IServiceCollection services,
    NpgsqlDataSource dataSource,
    bool useNodaTime,
    string schema)
    where TDbContext : DbContext
    {
        services.AddSingleton(dataSource);
        services.AddSingleton<InsertOutboxMessagesInterceptor>();
        services.AddDbContext<TDbContext>((sp, options) =>
            options
                .UseNpgsql(dataSource, npgsql =>
                {
                    npgsql.MigrationsHistoryTable(
                        "__EFMigrationsHistory",
                        schema);
                    if (useNodaTime)
                    {
                        npgsql.UseNodaTime();
                    }
                    npgsql.UseQuerySplittingBehavior(
                        QuerySplittingBehavior.SplitQuery);
                })
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<InsertOutboxMessagesInterceptor>()));
        return services;
    }
    private static string BuildConnectionString(DatabaseConnectionOptions config) => new NpgsqlConnectionStringBuilder
    {
        Database = config.Database,
        Host = config.Host,
        Port = config.Port,
        SslMode = SslMode.Require,
        Username = config.Username
    }.ConnectionString;
}
