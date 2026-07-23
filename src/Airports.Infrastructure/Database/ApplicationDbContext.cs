using Airports.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Airports.Infrastructure.Database;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }
    public DbSet<Airport> Airports { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var assembly = typeof(ApplicationDbContext).Assembly;
        modelBuilder.HasDefaultSchema("airports");
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}
