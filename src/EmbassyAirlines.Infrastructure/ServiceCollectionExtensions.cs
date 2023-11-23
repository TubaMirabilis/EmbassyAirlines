using EmbassyAirlines.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmbassyAirlines.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = "Host=" + config["NpgSqlConnection:Host"] +
            ";Username=" + config["NpgSqlConnection:Username"] +
            ";Password=" + config["NpgSqlConnection:Password"] +
            ";Database=" + config["NpgSqlConnection:Database"];
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        services.AddHealthChecks().AddNpgSql(connectionString);
        services.AddTransient<IFleetRepository, FleetRepository>();
        return services;
    }
}