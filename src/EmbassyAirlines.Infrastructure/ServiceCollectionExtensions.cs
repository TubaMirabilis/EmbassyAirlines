using EmbassyAirlines.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmbassyAirlines.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = "server=" + configuration["MySqlConnection:Server"] +
            ";user=" + configuration["MySqlConnection:User"] +
            ";password=" + configuration["MySqlConnection:Password"] +
            ";database=" + configuration["MySqlConnection:Database"];
        return services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        })
        .AddTransient<IFleetRepository, FleetRepository>();
    }
}