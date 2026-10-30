using Aircraft.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aircraft.Infrastructure.Database.Configurations;

internal sealed class CompletedFlightConfiguration : IEntityTypeConfiguration<CompletedFlight>
{
    public void Configure(EntityTypeBuilder<CompletedFlight> builder)
    {
        builder.Property(cf => cf.ArrivalAirportIcao).HasMaxLength(4).IsUnicode(false).IsRequired();
        builder.Property(cf => cf.DepartureAirportIcao).HasMaxLength(4).IsUnicode(false).IsRequired();
        builder.HasOne<Core.Models.Aircraft>().WithMany(a => a.CompletedFlights).HasForeignKey("AircraftId").IsRequired();
        builder.ComplexProperty(cf => cf.Milestones, b =>
        {
            b.Property(e => e.LandingAt).IsRequired();
            b.Property(e => e.OffBlockAt).IsRequired();
            b.Property(e => e.OnBlockAt).IsRequired();
            b.Property(e => e.TakeOffAt).IsRequired();
        });
    }
}
