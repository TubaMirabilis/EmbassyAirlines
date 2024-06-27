using Flights.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
        var flightEntity = modelBuilder.Entity<Flight>();
        ConfigureFlightTextProperties(flightEntity);
        ConfigureFlightAirportProperties(flightEntity);
    }
    private static void ConfigureFlightTextProperties(EntityTypeBuilder<Flight> flightEntity)
    {
        flightEntity.Property(f => f.Number).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.NumberIataFormat).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.NumberIcaoFormat).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.AircraftTypeDesignator).HasMaxLength(4).HasColumnType("varchar(4)");
        flightEntity.Property(f => f.AircraftRegistration).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.Status).HasConversion<string>().HasMaxLength(20).HasColumnType("varchar(20)");
        flightEntity.Property(f => f.Notes).HasMaxLength(500).HasColumnType("varchar(500)");
    }
    private static void ConfigureFlightAirportProperties(EntityTypeBuilder<Flight> flightEntity)
    {
        flightEntity.Property(f => f.DepartureGate).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.ArrivalGate).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.DepartureTerminal).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.ArrivalTerminal).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.DepartureTaf).HasMaxLength(500).HasColumnType("varchar(500)");
        flightEntity.Property(f => f.ArrivalTaf).HasMaxLength(500).HasColumnType("varchar(500)");
        flightEntity.Property(f => f.DepartureMetar).HasMaxLength(500).HasColumnType("varchar(500)");
        flightEntity.Property(f => f.ArrivalMetar).HasMaxLength(500).HasColumnType("varchar(500)");
    }
    public DbSet<Flight> Flights { get; set; }
    public DbSet<Airport> Airports { get; set; }
}
