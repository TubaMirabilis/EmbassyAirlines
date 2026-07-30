using Flights.Infrastructure;
using Flights.Infrastructure.Database;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Contracts;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Flights.Migrations.Lambda;

public sealed class Function
{
    public static async Task<MigrationResponse> FunctionHandler(MigrationRequest request, ILambdaContext context)
    {
        if (string.Equals(request.RequestType, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            return new MigrationResponse("flights-database-migrations");
        }
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddEnvironmentVariables("FLIGHTS_");
        builder.Services.AddDatabaseConnection(builder.Configuration);
        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Logger.LogInformation("Applying Flights database migrations.");
        await dbContext.Database.MigrateAsync();
        context.Logger.LogInformation("Flights database migrations completed.");
        return new MigrationResponse("flights-database-migrations");
    }
}
