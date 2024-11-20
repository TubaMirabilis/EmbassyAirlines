using Flights.Api.Domain.Flights;
using Flights.Api.Domain.Seats;
using Microsoft.EntityFrameworkCore;

namespace Flights.Api.Database;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }
    public required DbSet<Flight> Flights { get; set; }
    public required DbSet<Seat> Seats { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var assembly = typeof(ApplicationDbContext).Assembly;
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}
