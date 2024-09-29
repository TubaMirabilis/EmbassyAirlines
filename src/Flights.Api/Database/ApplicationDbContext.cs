using Flights.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flights.Api.Database;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }
    public DbSet<Flight> Flights { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Flight>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Departure)
                  .IsRequired()
                  .HasMaxLength(100);
            entity.Property(e => e.Destination)
                  .IsRequired()
                  .HasMaxLength(100);
            entity.Property(e => e.DepartureTime)
                  .IsRequired();
            entity.Property(e => e.ArrivalTime)
                  .IsRequired();
            entity.Property(e => e.Price)
                  .IsRequired();
            entity.Property(e => e.AvailableSeats)
                  .IsRequired();
        });
    }
}
