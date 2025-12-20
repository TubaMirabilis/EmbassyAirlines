using Flights.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Flights.Infrastructure.Database;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }
    public DbSet<Aircraft> Aircraft { get; set; } = null!;
    public DbSet<Airport> Airports { get; set; } = null!;
    public DbSet<Flight> Flights { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var assembly = typeof(ApplicationDbContext).Assembly;
        modelBuilder.HasDefaultSchema("flights");
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}
