using EaIdentity.Application;
using EaIdentity.Infrastructure;
using HealthChecks.UI.Client;
using Mediator;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var services = builder.Services;
config.AddEnvironmentVariables(prefix: "EAIDENTITY_");
services.AddApplication();
services.AddInfrastructure(config);
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.MapHealthChecks("/_health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.UseAuthorization();
app.MapControllers();
app.Run();