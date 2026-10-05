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
        dataSourceBuilder.UsePasswordProvider(
            passwordProvider: _ => throw new NotSupportedException("Use OpenAsync"),
            passwordProviderAsync: async (builder, ct) => await RDSAuthTokenGenerator.GenerateAuthTokenAsync(dbConfig.Host, dbConfig.Port, dbConfig.Username));
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
    internal static string BuildConnectionString(DatabaseConnectionOptions config) => new NpgsqlConnectionStringBuilder
    {
        Database = config.Database,
        Host = config.Host,
        Port = config.Port,
        SslMode = SslMode.Require,
        Username = config.Username
    }.ConnectionString;
}
