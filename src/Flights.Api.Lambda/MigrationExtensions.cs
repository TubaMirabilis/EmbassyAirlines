﻿using Flights.Api.Lambda.Database;
using Microsoft.EntityFrameworkCore;

namespace Flights.Api.Lambda;

internal static class MigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
