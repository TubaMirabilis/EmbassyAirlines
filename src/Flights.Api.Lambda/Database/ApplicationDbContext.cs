using Microsoft.EntityFrameworkCore;

namespace Flights.Api.Lambda.Database;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }
    public DbSet<Airport> Airports { get; set; } = null!;
    public DbSet<Flight> Flights { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var assembly = typeof(ApplicationDbContext).Assembly;
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}
