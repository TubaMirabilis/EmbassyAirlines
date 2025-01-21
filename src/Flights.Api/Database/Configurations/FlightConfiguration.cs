using Flights.Api.Domain.Flights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flights.Api.Database.Configurations;

internal sealed class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
       private const string Annotation = "Npgsql:CheckConstraint";
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
              builder.HasMany(b => b.Seats)
                     .WithOne()
                     .HasForeignKey(s => s.FlightId)
                      .OnDelete(DeleteBehavior.Cascade);
              builder.Navigation(f => f.Seats).AutoInclude();
       }
       private static void ConfigureSchedule(EntityTypeBuilder<Flight> builder)
           => builder.ComplexProperty(b => b.Schedule, schedule =>
           {
                  schedule.ComplexProperty(s => s.DepartureAirport, ConfigureDepartureAirport);
                  schedule.ComplexProperty(s => s.DestinationAirport, ConfigureDestinationAirport);
                  schedule.Property(s => s.DepartureTime)
                       .IsRequired();
                  schedule.Property(s => s.ArrivalTime)
                       .IsRequired();
           });
       private static void ConfigureDepartureAirport(ComplexPropertyBuilder<Airport> builder)
       {
              builder.Property(d => d.IataCode)
                     .IsRequired()
                     .HasMaxLength(3)
                     .IsUnicode(false)
                     .HasAnnotation(Annotation, "Departure = upper(Departure)");
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
                     .HasAnnotation(Annotation, "Destination = upper(Destination)");
              builder.Property(d => d.TimeZone)
                      .IsRequired()
                      .HasMaxLength(50)
                      .IsUnicode(false);
       }
}
