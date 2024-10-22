using Flights.Api.Domain.Flights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flights.Api.Configurations;

internal sealed class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    private const string _annotation = "Npgsql:CheckConstraint";
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.CreatedAt)
               .IsRequired();
        builder.Property(b => b.UpdatedAt)
               .IsRequired();
        builder.Property(b => b.FlightNumber)
               .IsRequired()
               .HasMaxLength(10)
               .IsUnicode(false);
        ConfigureSchedule(builder);
        builder.Navigation(f => f.Seats).AutoInclude();
    }
    private static void ConfigureSchedule(EntityTypeBuilder<Flight> builder)
    {
        builder.ComplexProperty(b => b.Schedule, schedule =>
        {
            schedule.ComplexProperty(schedule => schedule.DepartureAirport,
                departure => ConfigureDepartureAirport(departure));
            schedule.ComplexProperty(schedule => schedule.DestinationAirport,
                destination => ConfigureDestinationAirport(destination));
            schedule.Property(s => s.DepartureTime)
                    .IsRequired();
            schedule.Property(s => s.ArrivalTime)
                    .IsRequired();
        });
    }
    private static void ConfigureDepartureAirport(ComplexPropertyBuilder<Airport> builder)
    {
        builder.Property(d => d.IataCode)
               .IsRequired()
               .HasMaxLength(3)
               .IsUnicode(false)
               .HasAnnotation(_annotation, "Departure = upper(Departure)");
        builder.Property(d => d.TimeZone)
               .IsRequired()
               .HasMaxLength(50)
               .IsUnicode(false);
    }
    private static void ConfigureDestinationAirport(ComplexPropertyBuilder<Airport> builder)
    {
        builder.Property(d => d.IataCode)
               .IsRequired()
               .HasMaxLength(3)
               .IsUnicode(false)
               .HasAnnotation(_annotation, "Destination = upper(Destination)");
        builder.Property(d => d.TimeZone)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
    }
}
