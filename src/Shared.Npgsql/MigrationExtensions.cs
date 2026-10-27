using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Npgsql;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync<TDbContext>(this IHost app) where TDbContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TDbContext>>();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var startTime = Stopwatch.GetTimestamp();
        await dbContext.Database.MigrateAsync();
        var diff = Stopwatch.GetElapsedTime(startTime);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Applied database migrations for {DbContext} in {ElapsedMilliseconds} ms.", typeof(TDbContext).Name, diff.TotalMilliseconds);
        }
    }
}
