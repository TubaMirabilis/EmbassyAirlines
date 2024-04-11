using Flights.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flights.Api.Database;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<Flight>().Property(f => f.Number).HasMaxLength(10).HasColumnType("varchar(10)");
        modelBuilder.Entity<Flight>().Property(f => f.NumberIataFormat).HasMaxLength(10).HasColumnType("varchar(10)");
        modelBuilder.Entity<Flight>().Property(f => f.NumberIcaoFormat).HasMaxLength(10).HasColumnType("varchar(10)");
        modelBuilder.Entity<Flight>().Property(f => f.DepartureTimeZoneId).HasMaxLength(50).HasColumnType("varchar(50)");
        modelBuilder.Entity<Flight>().Property(f => f.ArrivalTimeZoneId).HasMaxLength(50).HasColumnType("varchar(50)");
        modelBuilder.Entity<Flight>().Property(f => f.AircraftTypeDesignator).HasMaxLength(4).HasColumnType("varchar(4)");
        modelBuilder.Entity<Flight>().Property(f => f.AircraftRegistration).HasMaxLength(10).HasColumnType("varchar(10)");
        modelBuilder.Entity<Flight>().Property(f => f.Status).HasConversion<string>().HasMaxLength(20).HasColumnType("varchar(20)");
        modelBuilder.Entity<Flight>().Property(f => f.DepartureGate).HasMaxLength(10).HasColumnType("varchar(10)");
        modelBuilder.Entity<Flight>().Property(f => f.ArrivalGate).HasMaxLength(10).HasColumnType("varchar(10)");
        modelBuilder.Entity<Flight>().Property(f => f.DepartureTerminal).HasMaxLength(10).HasColumnType("varchar(10)");
        modelBuilder.Entity<Flight>().Property(f => f.ArrivalTerminal).HasMaxLength(10).HasColumnType("varchar(10)");
        modelBuilder.Entity<Flight>().Property(f => f.DepartureAirportIata).HasMaxLength(3).HasColumnType("varchar(3)");
        modelBuilder.Entity<Flight>().Property(f => f.ArrivalAirportIata).HasMaxLength(3).HasColumnType("varchar(3)");
        modelBuilder.Entity<Flight>().Property(f => f.DepartureAirportIcao).HasMaxLength(4).HasColumnType("varchar(4)");
        modelBuilder.Entity<Flight>().Property(f => f.ArrivalAirportIcao).HasMaxLength(4).HasColumnType("varchar(4)");
        modelBuilder.Entity<Flight>().Property(f => f.Notes).HasMaxLength(500).HasColumnType("varchar(500)");
        modelBuilder.Entity<Flight>().Property(f => f.DepartureTaf).HasMaxLength(500).HasColumnType("varchar(500)");
        modelBuilder.Entity<Flight>().Property(f => f.ArrivalTaf).HasMaxLength(500).HasColumnType("varchar(500)");
        modelBuilder.Entity<Flight>().Property(f => f.DepartureMetar).HasMaxLength(500).HasColumnType("varchar(500)");
        modelBuilder.Entity<Flight>().Property(f => f.ArrivalMetar).HasMaxLength(500).HasColumnType("varchar(500)");
        modelBuilder.Entity<Flight>().Ignore(f => f.TotalPassengers);
        modelBuilder.Entity<Flight>().Ignore(f => f.Duration);
        modelBuilder.Entity<Flight>().Ignore(f => f.DepartureTimeLocal);
        modelBuilder.Entity<Flight>().Ignore(f => f.ArrivalTimeLocal);
        modelBuilder.Entity<Flight>().Ignore(f => f.DepartureTimeLocalString);
        modelBuilder.Entity<Flight>().Ignore(f => f.ArrivalTimeLocalString);
    }
    public DbSet<Flight> Flights { get; set; }
}
