using EaIdentity.Application.Services;
using EaIdentity.Infrastructure.Data;
using EaIdentity.Infrastructure.Options;
using EaIdentity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EaIdentity.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = "Host=" + configuration["NpgSqlConnection:Host"] +
            ";Username=" + configuration["NpgSqlConnection:Username"] +
            ";Password=" + configuration["NpgSqlConnection:Password"] +
            ";Database=" + configuration["NpgSqlConnection:Database"];
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        var secret = configuration["JwtSettings:Secret"];
        var tokenLifetimeStr = configuration["JwtSettings:TokenLifetime"];
        if (secret is not null && TimeSpan.TryParse(tokenLifetimeStr, out var tokenLifetime))
        {
            var jwtSettings = new JwtSettings
            {
                Secret = secret,
                TokenLifetime = tokenLifetime
            };
            services.AddSingleton(jwtSettings);
        }
        else
        {
            throw new Exception("The JWT settings are not configured properly");
        }
        services.AddScoped<IIdentityService, IdentityService>();
        return services;
    }
}