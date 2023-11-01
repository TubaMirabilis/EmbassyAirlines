using EmbassyAirlines.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmbassyAirlines.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = "Host=" + configuration["NpgSqlConnection:Host"] +
            ";Username=" + configuration["NpgSqlConnection:Username"] +
            ";Password=" + configuration["NpgSqlConnection:Password"] +
            ";Database=" + configuration["NpgSqlConnection:Database"];
        return services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        })
        .AddTransient<IFleetRepository, FleetRepository>();
    }
}