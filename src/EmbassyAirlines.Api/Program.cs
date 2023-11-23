using System.Text;
using EmbassyAirlines.Api.Apis;
using EmbassyAirlines.Api.Swagger;
using EmbassyAirlines.Application;
using EmbassyAirlines.Infrastructure;
using HealthChecks.UI.Client;
using Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var services = builder.Services;
var key = config["JwtSettings:Key"] ?? throw new Exception("The JWT settings are not configured properly");
var keyAsBytes = Encoding.UTF8.GetBytes(key);
config.AddEnvironmentVariables(prefix: "EMBASSYAIRLINES_");
services.AddApplication();
services.AddInfrastructure(config);
services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", b =>
        b.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader());
});
services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = config["JwtSettings:Issuer"],
        ValidAudience = config["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(keyAsBytes),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
services.AddOutputCache()
    .AddStackExchangeRedisCache(options =>
    {
        options.Configuration = config["Redis:ConnectionString"];
        options.InstanceName = config["Redis:InstanceName"];
    });
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.MapHealthChecks("/_health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.UseOutputCache();
app.MapFleetApi();
app.Run();