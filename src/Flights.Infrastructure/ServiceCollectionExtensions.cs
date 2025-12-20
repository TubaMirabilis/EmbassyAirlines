using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Flights.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseConnection(this IServiceCollection services, IConfiguration config)
    {
        var host = config["DbConnection:Host"];
        var dbName = config["DbConnection:Database"];
        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Database = dbName
        }.ConnectionString;
        services.AddSingleton<EntityFrameworkInterceptor>();
        services.AddDbContext<ApplicationDbContext>((sp, options) => options.UseNpgsql(new NpgsqlConnection(connectionString), x =>
        {
            x.MigrationsHistoryTable("__EFMigrationsHistory", "flights");
            x.UseNodaTime();
            x.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        })
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(sp.GetRequiredService<EntityFrameworkInterceptor>()));
        return services;
    }
}
