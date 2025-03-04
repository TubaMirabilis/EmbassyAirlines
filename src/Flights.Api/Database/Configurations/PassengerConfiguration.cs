using Flights.Api.Domain.Passengers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flights.Api.Database.Configurations;

internal sealed class PassengerConfiguration : IEntityTypeConfiguration<Passenger>
{
    public void Configure(EntityTypeBuilder<Passenger> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.FirstName)
               .HasMaxLength(50)
               .IsRequired();
        builder.Property(b => b.LastName)
               .HasMaxLength(50)
               .IsRequired();
    }
}
