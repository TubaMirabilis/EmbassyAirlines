using Aircraft.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Aircraft.Infrastructure.Database.Configurations;

internal sealed class AircraftConfiguration : IEntityTypeConfiguration<Core.Models.Aircraft>
{
    public void Configure(EntityTypeBuilder<Core.Models.Aircraft> builder)
    {
        builder.Property(a => a.TailNumber)
               .IsRequired()
                .IsUnicode(false)
               .HasMaxLength(12);
        builder.Property(a => a.EquipmentCode)
               .IsRequired()
               .IsUnicode(false)
               .HasMaxLength(4);
        builder.Property<uint>("xmin").IsRowVersion();
        builder.ComplexProperty(a => a.DryOperatingWeight, b => b.Property(e => e.Kilograms).IsRequired());
        builder.ComplexProperty(a => a.MaximumTakeoffWeight, b => b.Property(e => e.Kilograms).IsRequired());
        builder.ComplexProperty(a => a.MaximumLandingWeight, b => b.Property(e => e.Kilograms).IsRequired());
        builder.ComplexProperty(a => a.MaximumZeroFuelWeight, b => b.Property(e => e.Kilograms).IsRequired());
        builder.ComplexProperty(a => a.MaximumFuelWeight, b => b.Property(e => e.Kilograms).IsRequired());
        builder.ComplexProperty(a => a.Situation, situation =>
        {
            situation.Property(s => s.Status)
        .HasConversion(new EnumToStringConverter<Status>())
        .HasMaxLength(12)
        .IsUnicode(false)
        .IsRequired();
            situation.Property(s => s.ParkedAt)
        .HasMaxLength(4)
        .IsUnicode(false)
        .IsRequired(false);
            situation.Property(s => s.EnRouteTo)
        .HasMaxLength(4)
        .IsUnicode(false)
        .IsRequired(false);
            situation.Property(s => s.EffectiveAt)
        .IsRequired();
        });
        builder.ToTable(t => t.HasCheckConstraint(
            "ck_aircraft_situation",
            """
            (
                situation_status = 'Parked'
                AND situation_parked_at IS NOT NULL
                AND btrim(situation_parked_at) <> ''
                AND situation_en_route_to IS NULL
            )
            OR
            (
                situation_status = 'EnRoute'
                AND situation_en_route_to IS NOT NULL
                AND btrim(situation_en_route_to) <> ''
                AND situation_parked_at IS NULL
            )
            """));
        builder.Ignore(a => a.DomainEvents);
    }
}
