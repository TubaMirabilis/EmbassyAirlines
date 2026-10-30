using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Flights.Core.Models;
using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Shared.Contracts;

namespace Flights.Api.Lambda.FunctionalTests;

public sealed class FlightMilestoneTests : BaseFunctionalTest
{
    private static readonly DateTimeZone _incheonTimeZone = DateTimeZoneProviders.Tzdb["Asia/Seoul"];
    private static readonly DateTimeZone _schipholTimeZone = DateTimeZoneProviders.Tzdb["Europe/Amsterdam"];
    private static readonly Duration _flightDuration = Duration.FromHours(10) + Duration.FromMinutes(30);
    private readonly FunctionalTestWebAppFactory _factory;
    private readonly Airport _incheon;
    private readonly Airport _schiphol;
    private readonly Aircraft _aircraft;
    private readonly IClock _clock;

    public FlightMilestoneTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
        ArgumentNullException.ThrowIfNull(factory.IncheonAirport);
        ArgumentNullException.ThrowIfNull(factory.SchipholAirport);
        ArgumentNullException.ThrowIfNull(factory.Aircraft1);
        _factory = factory;
        _incheon = factory.IncheonAirport;
        _schiphol = factory.SchipholAirport;
        _aircraft = factory.Aircraft1;
        _clock = factory.Services.GetRequiredService<IClock>();
    }

    [Fact]
    public async Task RecordMilestone_Should_NotMoveUpdatedAtBackwards_WhenMilestonesAreRecordedRetrospectively()
    {
        var ct = TestContext.Current.CancellationToken;
        var flightId = await ScheduleFlightAsync(ct);
        var previousUpdatedAt = (await LoadFlightAsync(flightId, ct)).UpdatedAt;

        // Every milestone happened before the flight was even scheduled. Whole-minute
        // instants round-trip through PostgreSQL's microsecond precision unchanged.
        var offBlockAt = TimeHelpers.MinutesFromNowRoundedUp(_clock, -60);
        (FlightMilestone Milestone, Instant OccurredAt)[] milestones =
        [
            (FlightMilestone.OffBlock, offBlockAt),
            (FlightMilestone.Takeoff, offBlockAt + Duration.FromMinutes(10)),
            (FlightMilestone.Landing, offBlockAt + Duration.FromMinutes(40)),
            (FlightMilestone.OnBlock, offBlockAt + Duration.FromMinutes(50)),
        ];

        foreach (var (milestone, occurredAt) in milestones)
        {
            var requestedAt = _clock.GetCurrentInstant();
            var request = new RecordFlightMilestoneDto(milestone.ToString(), occurredAt.ToDateTimeOffset());
            var response = await HttpClient.PatchAsJsonAsync($"flights/{flightId}/milestones", request, ct);
            response.EnsureSuccessStatusCode();

            var flight = await LoadFlightAsync(flightId, ct);
            GetMilestoneAt(flight, milestone).Should().Be(occurredAt);
            flight.UpdatedAt.Should().BeGreaterThanOrEqualTo(requestedAt);
            flight.UpdatedAt.Should().BeGreaterThanOrEqualTo(previousUpdatedAt);
            previousUpdatedAt = flight.UpdatedAt;
        }

        (await LoadFlightAsync(flightId, ct)).Status.Should().Be(FlightStatus.Arrived);
    }

    private static Instant? GetMilestoneAt(Flight flight, FlightMilestone milestone) => milestone switch
    {
        FlightMilestone.OffBlock => flight.OffBlockAt,
        FlightMilestone.Takeoff => flight.TakeoffAt,
        FlightMilestone.Landing => flight.LandingAt,
        FlightMilestone.OnBlock => flight.OnBlockAt,
        _ => throw new ArgumentOutOfRangeException(nameof(milestone))
    };

    private async Task<Flight> LoadFlightAsync(Guid flightId, CancellationToken ct)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Flights
                       .AsNoTracking()
                       .SingleAsync(f => f.Id == flightId, ct);
    }

    private async Task<Guid> ScheduleFlightAsync(CancellationToken ct)
    {
        var departureInstant = TimeHelpers.MinutesFromNowRoundedUp(_clock, 30);
        var arrivalInstant = departureInstant + _flightDuration;
        var request = new ScheduleFlightDto
        {
            AircraftId = _aircraft.Id,
            FlightNumberIata = "EB2",
            FlightNumberIcao = "EBY2",
            DepartureAirportId = _incheon.Id,
            DepartureLocalTime = departureInstant.InZone(_incheonTimeZone).ToDateTimeUnspecified(),
            ArrivalAirportId = _schiphol.Id,
            ArrivalLocalTime = arrivalInstant.InZone(_schipholTimeZone).ToDateTimeUnspecified(),
            EconomyPrice = 400,
            BusinessPrice = 4000,
            SchedulingAmbiguityPolicy = "ThrowWhenAmbiguous",
            OperationType = "RevenuePassenger"
        };
        var response = await HttpClient.PostAsJsonAsync("flights", request, ct);
        response.EnsureSuccessStatusCode();
        var flight = await response.Content.ReadFromJsonAsync<FlightDto>(ct);
        return flight?.Id ?? throw new JsonException();
    }
}
