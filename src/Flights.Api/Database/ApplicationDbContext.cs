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
        ConfigureFlightTimeZoneProperties(flightEntity);
        ConfigureFlightAirportProperties(flightEntity);
        ConfigureIgnoredFlightProperties(flightEntity);
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
    private static void ConfigureFlightTimeZoneProperties(EntityTypeBuilder<Flight> flightEntity)
    {
        flightEntity.Property(f => f.DepartureTimeZoneId).HasMaxLength(50).HasColumnType("varchar(50)");
        flightEntity.Property(f => f.ArrivalTimeZoneId).HasMaxLength(50).HasColumnType("varchar(50)");
    }
    private static void ConfigureFlightAirportProperties(EntityTypeBuilder<Flight> flightEntity)
    {
        flightEntity.Property(f => f.DepartureGate).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.ArrivalGate).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.DepartureTerminal).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.ArrivalTerminal).HasMaxLength(10).HasColumnType("varchar(10)");
        flightEntity.Property(f => f.DepartureAirportIata).HasMaxLength(3).HasColumnType("varchar(3)");
        flightEntity.Property(f => f.ArrivalAirportIata).HasMaxLength(3).HasColumnType("varchar(3)");
        flightEntity.Property(f => f.DepartureAirportIcao).HasMaxLength(4).HasColumnType("varchar(4)");
        flightEntity.Property(f => f.ArrivalAirportIcao).HasMaxLength(4).HasColumnType("varchar(4)");
        flightEntity.Property(f => f.DepartureTaf).HasMaxLength(500).HasColumnType("varchar(500)");
        flightEntity.Property(f => f.ArrivalTaf).HasMaxLength(500).HasColumnType("varchar(500)");
        flightEntity.Property(f => f.DepartureMetar).HasMaxLength(500).HasColumnType("varchar(500)");
        flightEntity.Property(f => f.ArrivalMetar).HasMaxLength(500).HasColumnType("varchar(500)");
    }
    private static void ConfigureIgnoredFlightProperties(EntityTypeBuilder<Flight> flightEntity)
    {
        flightEntity.Ignore(f => f.TotalPassengers);
        flightEntity.Ignore(f => f.Duration);
        flightEntity.Ignore(f => f.DepartureTimeLocal);
        flightEntity.Ignore(f => f.ArrivalTimeLocal);
        flightEntity.Ignore(f => f.DepartureTimeLocalString);
        flightEntity.Ignore(f => f.ArrivalTimeLocalString);
    }
    public DbSet<Flight> Flights { get; set; }
}
