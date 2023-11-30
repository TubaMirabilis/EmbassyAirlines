using System.Text;
using EaIdentity.Application.Services;
using EaIdentity.Infrastructure.Data;
using EaIdentity.Infrastructure.Identity;
using EaIdentity.Infrastructure.Options;
using EaIdentity.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

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
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireDigit = false;
            options.SignIn.RequireConfirmedAccount = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ";
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();        services.AddHealthChecks().AddNpgSql(connectionString);
        var secret = config["JwtSettings:Secret"];
        var tokenLifetimeStr = config["JwtSettings:TokenLifetime"];
        var issuer = config["JwtSettings:Issuer"];
        var audience = config["JwtSettings:Audience"];
        if (string.IsNullOrEmpty(secret) ||
            !TimeSpan.TryParse(tokenLifetimeStr, out var tokenLifetime) ||
            string.IsNullOrEmpty(issuer) ||
            string.IsNullOrEmpty(audience))
        {
            throw new Exception("The JWT settings are not configured properly");
        }
        var secretAsBytes = Encoding.UTF8.GetBytes(secret);
        JwtSettings jwtSettings = new(secret, tokenLifetime, issuer, audience);
        services.AddSingleton(jwtSettings);
        TokenValidationParameters tokenValidationParameters = new
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(secretAsBytes),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
        services.AddSingleton(tokenValidationParameters);
        services.AddScoped<IIdentityService, IdentityService>();
        return services;
    }
}