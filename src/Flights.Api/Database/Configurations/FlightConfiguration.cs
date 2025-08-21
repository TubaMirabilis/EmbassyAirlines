using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flights.Api.Database.Configurations;

internal sealed class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.ComplexProperty(f => f.BusinessPrice, b => b.Property(e => e.Amount).IsRequired());
        builder.ComplexProperty(f => f.EconomyPrice, b => b.Property(e => e.Amount).IsRequired());
        builder.Property(f => f.FlightNumberIcao)
            .HasMaxLength(7)
            .IsRequired();
        builder.Property(f => f.FlightNumberIata)
            .HasMaxLength(6)
            .IsRequired();
        builder.Navigation(f => f.Aircraft)
            .AutoInclude();
        builder.Navigation(f => f.DepartureAirport)
            .AutoInclude();
        builder.Navigation(f => f.ArrivalAirport)
            .AutoInclude();
        builder.HasOne(f => f.DepartureAirport)
            .WithMany()
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.ArrivalAirport)
            .WithMany()
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.Aircraft)
            .WithMany()
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
