using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.EntityFrameworkCore;

public static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync<TDbContext>(this IApplicationBuilder app) where TDbContext : DbContext
    {
        using var scope = app.ApplicationServices.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
