using System.Text.Json;
using Flights.Core.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Shared;
using Shared.Contracts;

namespace Flights.Infrastructure.IntegrationTests;

[Collection("Postgres")]
public sealed class FlightOptimisticConcurrencyTests
{
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly PostgreSqlFixture _postgres;
    public FlightOptimisticConcurrencyTests(PostgreSqlFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task SaveChangesAsync_should_throw_when_flight_was_modified_by_another_context()
    {
        var now = Instant.FromUtc(2026, 1, 1, 9, 0);
        var aircraft = Aircraft.Create(Guid.CreateVersion7(), "C-FJRN", "B78X", now);
        var incheonAirport = Airport.Create(new AirportCreationArgs
        {
            CreatedAt = now,
            IataCode = "ICN",
            IcaoCode = "RKSI",
            Id = Guid.NewGuid(),
            Name = "Incheon International Airport",
            TimeZoneId = "Asia/Seoul"
        });
        var schipholAirport = Airport.Create(new AirportCreationArgs
        {
            CreatedAt = now,
            IataCode = "AMS",
            IcaoCode = "EHAM",
            Id = Guid.NewGuid(),
            Name = "Schiphol Airport",
            TimeZoneId = "Europe/Amsterdam"
        });
        var flight = FlightTestData.Create(aircraft, incheonAirport, schipholAirport, now);

        await using (var db = _postgres.CreateDbContext())
        {
            db.Add(aircraft);
            db.Add(incheonAirport);
            db.Add(schipholAirport);
            db.Add(flight);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db1 = _postgres.CreateDbContext();
        await using var db2 = _postgres.CreateDbContext();

        var flight1 = await db1.Flights.SingleAsync(f => f.Id == flight.Id, TestContext.Current.CancellationToken);
        var flight2 = await db2.Flights.SingleAsync(f => f.Id == flight.Id, TestContext.Current.CancellationToken);

        flight1.AdjustPricing(new Money(500), new Money(1000), now + Duration.FromMinutes(1));
        await db1.SaveChangesAsync(TestContext.Current.CancellationToken);

        flight2.AdjustPricing(new Money(600), new Money(1200), now + Duration.FromMinutes(2));

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () => await db2.SaveChangesAsync(TestContext.Current.CancellationToken));

        await using var verificationDb = _postgres.CreateDbContext();

        var pricingMessages = await verificationDb
            .Set<OutboxMessage>()
            .AsNoTracking()
            .Where(m => m.Name == nameof(FlightPricingAdjustedEvent))
            .Select(m => m.Content)
            .ToListAsync(TestContext.Current.CancellationToken);
        var pricingEvents = pricingMessages
            .Select(content => JsonSerializer.Deserialize<FlightPricingAdjustedEvent>(content, s_jsonSerializerOptions))
            .OfType<FlightPricingAdjustedEvent>()
            .Where(e => e.FlightId == flight.Id)
            .ToList();

        var persistedEvent = Assert.Single(pricingEvents);

        Assert.Equal(500, persistedEvent.EconomyPrice);
        Assert.Equal(1000, persistedEvent.BusinessPrice);
        var persisted = await verificationDb.Flights
            .AsNoTracking()
            .SingleAsync(
                f => f.Id == flight.Id,
                TestContext.Current.CancellationToken);

        Assert.Equal(new Money(500), persisted.EconomyPrice);
        Assert.Equal(new Money(1000), persisted.BusinessPrice);
    }
}
