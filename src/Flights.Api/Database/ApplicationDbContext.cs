using Flights.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
            entity.Property(e => e.CreatedAt)
                  .IsRequired();
            entity.Property(e => e.UpdatedAt)
                  .IsRequired();
            entity.Property(e => e.FlightNumber)
                  .IsRequired()
                  .HasMaxLength(10)
                  .IsUnicode(false);
            entity.ComplexProperty(e => e.Schedule, schedule =>
            {
                schedule.Property(e => e.Departure)
                        .IsRequired()
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasAnnotation("Npgsql:CheckConstraint", "Departure = upper(Departure)");
                schedule.Property(e => e.Destination)
                        .IsRequired()
                        .HasMaxLength(10)
                        .IsUnicode(false)
                        .HasAnnotation("Npgsql:CheckConstraint", "Destination = upper(Destination)");
                schedule.Property(e => e.DepartureTime)
                        .IsRequired();
                schedule.Property(e => e.ArrivalTime)
                        .IsRequired();
            });
            entity.ComplexProperty(e => e.Pricing, pricing =>
            {
                pricing.Property(e => e.EconomyPrice)
                       .IsRequired();
                pricing.Property(e => e.BusinessPrice)
                       .IsRequired();
            });
            entity.ComplexProperty(e => e.AvailableSeats, a =>
            {
                a.Property(e => e.Economy)
                             .IsRequired();
                a.Property(e => e.Business)
                             .IsRequired();
            });
        });
    }
}
