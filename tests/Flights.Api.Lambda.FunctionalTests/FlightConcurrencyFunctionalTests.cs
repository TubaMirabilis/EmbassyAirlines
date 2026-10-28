using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Flights.Core.Models;
using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Shared.Contracts;
using Shared.HttpApiLambdaDefaults.Extensions;
using Shared.TransactionalOutbox;

namespace Flights.Api.Lambda.FunctionalTests;

public sealed class FlightConcurrencyFunctionalTests : IClassFixture<ConcurrencyFunctionalTestWebAppFactory>
{
    private const string ConcurrencyConflictDetail = "The resource has changed or is no longer available. Refresh its state before retrying the operation.";
    private static readonly DateTimeZone _incheonTimeZone = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
    private static readonly DateTimeZone _schipholTimeZone = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
    private static readonly Duration _flightDuration = Duration.FromHours(10) + Duration.FromMinutes(30);
    private readonly ConcurrencyFunctionalTestWebAppFactory _factory;
    private readonly HttpClient _client;
    private readonly Airport _incheon;
    private readonly Airport _schiphol;
    private readonly Aircraft _aircraft;
    private readonly IClock _clock;

    public FlightConcurrencyFunctionalTests(
        ConcurrencyFunctionalTestWebAppFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory.IncheonAirport);
        ArgumentNullException.ThrowIfNull(factory.SchipholAirport);
        ArgumentNullException.ThrowIfNull(factory.Aircraft1);
        _factory = factory;
        _client = factory.CreateClient();
        _incheon = factory.IncheonAirport;
        _schiphol = factory.SchipholAirport;
        _aircraft = factory.Aircraft1;
        _clock = factory.Services.GetRequiredService<IClock>();
    }

    [Fact]
    public async Task AdjustFlightPricing_should_return_conflict_when_flight_was_modified_concurrently()
    {
        var ct = TestContext.Current.CancellationToken;
        var flight = await ScheduleFlightAsync(ct);
        var competingPricing = new AdjustFlightPricingDto(450, 4500);
        _factory.Services
                .GetRequiredService<ForceFlightConcurrencyConflictInterceptor>()
                .Arm(flight.Id, (flightId, token) => AdjustPricingOutOfBandAsync(flightId, competingPricing, token));

        var response = await _client.PatchAsJsonAsync(
            $"flights/{flight.Id}/pricing",
            new AdjustFlightPricingDto(500, 5000),
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var expected = response.StatusCode.CreateExpectedProblemDetails(ConcurrencyConflictDetail);
        var actual = await response.ReadProblemDetailsAsync();
        actual.Should().BeEquivalentTo(expected, options => options.Excluding(p => p.Extensions));

        // The competing write must survive, and the rejected write must leave no trace in the outbox
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var persisted = await db.Flights
                                .AsNoTracking()
                                .SingleAsync(f => f.Id == flight.Id, ct);
        persisted.EconomyPrice.Amount.Should().Be(competingPricing.EconomyPrice);
        persisted.BusinessPrice.Amount.Should().Be(competingPricing.BusinessPrice);
        var pricingEventContents = await db.Set<OutboxMessage>()
                                           .AsNoTracking()
                                           .Where(m => m.Name == nameof(FlightPricingAdjustedEvent))
                                           .Select(m => m.Content)
                                           .ToListAsync(ct);
        pricingEventContents
            .Select(content => JsonSerializer.Deserialize<FlightPricingAdjustedEvent>(content, JsonSerializerOptions.Web))
            .Where(e => e?.FlightId == flight.Id)
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Match<FlightPricingAdjustedEvent>(e =>
                e.EconomyPrice == competingPricing.EconomyPrice &&
                e.BusinessPrice == competingPricing.BusinessPrice);
    }

    private async Task<FlightDto> ScheduleFlightAsync(CancellationToken ct)
    {
        var departureInstant = TimeHelpers.MinutesFromNowRoundedUp(_clock, 30);
        var arrivalInstant = departureInstant + _flightDuration;
        var request = new ScheduleFlightDto
        {
            AircraftId = _aircraft.Id,
            FlightNumberIata = "EB1",
            FlightNumberIcao = "EBY1",
            DepartureAirportId = _incheon.Id,
            DepartureLocalTime = departureInstant.InZone(_incheonTimeZone).ToDateTimeUnspecified(),
            ArrivalAirportId = _schiphol.Id,
            ArrivalLocalTime = arrivalInstant.InZone(_schipholTimeZone).ToDateTimeUnspecified(),
            EconomyPrice = 400,
            BusinessPrice = 4000,
            SchedulingAmbiguityPolicy = "ThrowWhenAmbiguous",
            OperationType = "RevenuePassenger"
        };
        var response = await _client.PostAsJsonAsync("flights", request, ct);
        response.EnsureSuccessStatusCode();
        var flight = await response.Content.ReadFromJsonAsync<FlightDto>(ct);
        return flight ?? throw new JsonException();
    }

    private async Task AdjustPricingOutOfBandAsync(Guid flightId, AdjustFlightPricingDto pricing, CancellationToken ct)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var flight = await db.Flights.SingleAsync(f => f.Id == flightId, ct);
        flight.AdjustPricing(new Money(pricing.EconomyPrice), new Money(pricing.BusinessPrice), _clock.GetCurrentInstant());
        await db.SaveChangesAsync(ct);
    }
}
