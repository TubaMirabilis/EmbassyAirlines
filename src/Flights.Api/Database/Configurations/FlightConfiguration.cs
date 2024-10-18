using Flights.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flights.Api.Configurations;

internal sealed class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();
        builder.Property(b => b.FlightNumber).IsRequired().HasMaxLength(10).IsUnicode(false);
        builder.ComplexProperty(b => b.Schedule, schedule =>
        {
            schedule.Property(s => s.Departure).IsRequired().HasMaxLength(10).IsUnicode(false).HasAnnotation("Npgsql:CheckConstraint", "Departure = upper(Departure)");
            schedule.Property(s => s.Destination).IsRequired().HasMaxLength(10).IsUnicode(false).HasAnnotation("Npgsql:CheckConstraint", "Destination = upper(Destination)");
            schedule.Property(s => s.DepartureTime).IsRequired();
            schedule.Property(s => s.ArrivalTime).IsRequired();
        });
        builder.ComplexProperty(b => b.Pricing, pricing =>
        {
            pricing.Property(p => p.EconomyPrice).IsRequired();
            pricing.Property(p => p.BusinessPrice).IsRequired();
        });
        builder.ComplexProperty(b => b.AvailableSeats, a =>
        {
            a.Property(a => a.Economy).IsRequired();
            a.Property(a => a.Business).IsRequired();
        });
    }
}
