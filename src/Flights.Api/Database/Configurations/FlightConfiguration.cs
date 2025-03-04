using Flights.Api.Domain.Flights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flights.Api.Database.Configurations;

internal sealed class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.HasKey(b => b.Id);
        builder.HasOne(b => b.DepartureAirport)
               .WithMany()
               .HasForeignKey(b => b.DepartureAirportId)
               .IsRequired();
        builder.HasOne(b => b.ArrivalAirport)
               .WithMany()
               .HasForeignKey(b => b.ArrivalAirportId)
               .IsRequired();
        builder.Property(b => b.CreatedAt)
               .IsRequired();
        builder.Property(b => b.UpdatedAt)
               .IsRequired();
        builder.Property(b => b.FlightNumber)
               .IsRequired()
               .HasMaxLength(6)
               .IsUnicode(false);
        builder.Navigation(f => f.Seats)
               .AutoInclude();
        builder.Navigation(f => f.DepartureAirport)
               .AutoInclude();
        builder.Navigation(f => f.ArrivalAirport)
               .AutoInclude();
    }
}
