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
            schedule.ComplexProperty(schedule => schedule.DepartureAirport, departure =>
            {
                departure.Property(d => d.IataCode).IsRequired().HasMaxLength(3).IsUnicode(false).HasAnnotation("Npgsql:CheckConstraint", "Departure = upper(Departure)");
                departure.Property(d => d.TimeZone).IsRequired().HasMaxLength(50);
            });
            schedule.ComplexProperty(schedule => schedule.DestinationAirport, destination =>
            {
                destination.Property(d => d.IataCode).IsRequired().HasMaxLength(3).IsUnicode(false).HasAnnotation("Npgsql:CheckConstraint", "Destination = upper(Destination)");
                destination.Property(d => d.TimeZone).IsRequired().HasMaxLength(50);
            });
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
