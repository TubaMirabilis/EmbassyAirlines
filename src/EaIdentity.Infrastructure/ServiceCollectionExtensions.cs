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
        var secret = config["JwtSettings:Secret"];
        var tokenLifetimeStr = config["JwtSettings:TokenLifetime"];
        var issuer = config["JwtSettings:Issuer"];
        var audience = config["JwtSettings:Audience"];
        if (secret is not null &&
            TimeSpan.TryParse(tokenLifetimeStr, out var tokenLifetime) &&
            issuer is not null &&
            audience is not null)
        {
            var jwtSettings = new JwtSettings
            {
                Secret = secret,
                TokenLifetime = tokenLifetime,
                Issuer = issuer,
                Audience = audience
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