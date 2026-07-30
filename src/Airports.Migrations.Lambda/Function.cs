using Airports.Infrastructure.Database;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Contracts;
using Shared.Npgsql;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace Airports.Migrations.Lambda;

public sealed class Function
{
    public static async Task<MigrationResponse> FunctionHandler(MigrationRequest request, ILambdaContext context)
    {
        if (string.Equals(request.RequestType, "Delete", StringComparison.OrdinalIgnoreCase))
        {
            return new MigrationResponse("airports-database-migrations");
        }
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddEnvironmentVariables("AIRPORTS_");
        builder.Services.AddDatabaseConnection<ApplicationDbContext>(builder.Configuration, false, "airports");
        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Logger.LogInformation("Applying Airports database migrations.");
        await dbContext.Database.MigrateAsync();
        context.Logger.LogInformation("Airports database migrations completed.");
        return new MigrationResponse("airports-database-migrations");
    }
}
