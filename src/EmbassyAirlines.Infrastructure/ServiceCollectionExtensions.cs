using EmbassyAirlines.Application.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmbassyAirlines.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var redisConfig = config["Redis:ConnectionString"];
        ArgumentException.ThrowIfNullOrEmpty(redisConfig);
        var connectionString = "Host=" + config["NpgSqlConnection:Host"] +
            ";Port=" + config["NpgSqlConnection:Port"] +
            ";Database=" + config["NpgSqlConnection:Database"] +
            ";Username=" + config["NpgSqlConnection:Username"] +
            ";Password=" + config["NpgSqlConnection:Password"];
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddDataProtection().PersistKeysToDbContext<ApplicationDbContext>();
        services.AddHealthChecks().AddNpgSql(connectionString);
        services.AddHealthChecks().AddRedis(redisConfig);
        services.AddTransient<IFleetRepository, FleetRepository>();
        return services;
    }
}