using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests;

[TestCaseOrderer(typeof(PriorityOrderer))]
public class FlightsTests : BaseFunctionalTest
{
    private static FlightDto? _dto;
    private readonly Airport _incheon;
    private readonly Airport _schipol;
    private readonly Aircraft _aircraft;
    public FlightsTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
        ArgumentNullException.ThrowIfNull(factory.IncheonAirport);
        ArgumentNullException.ThrowIfNull(factory.SchipolAirport);
        ArgumentNullException.ThrowIfNull(factory.Aircraft);
        _incheon = factory.IncheonAirport;
        _schipol = factory.SchipolAirport;
        _aircraft = factory.Aircraft;
    }

    [Fact, TestPriority(0)]
    public async Task Create_Should_ReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var soon = DateTime.UtcNow.AddMinutes(30);
        var departureFromIncheon = soon.AddHours(8);
        var arrivalAtSchipol = soon.AddHours(3).AddMinutes(30);
        var duration = TimeSpan.FromHours(10).Add(TimeSpan.FromMinutes(30));
        var request = new CreateOrUpdateFlightDto(_aircraft.Id, "EB1", "EBY1", _incheon.Id, departureFromIncheon, _schipol.Id, arrivalAtSchipol, 400, 4000);

        // Act
        var response = await HttpClient.PostAsJsonAsync("flights", request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        _dto = await JsonSerializer.DeserializeAsync<FlightDto>(content, JsonSerializerOptions, TestContext.Current.CancellationToken) ?? throw new JsonException();

        // Assert
        _dto.Should().Match<FlightDto>(x =>
            x.FlightNumberIata == request.FlightNumberIata &&
            x.FlightNumberIcao == request.FlightNumberIcao &&
            x.DepartureAirportId == request.DepartureAirportId &&
            x.DepartureAirportIataCode == _incheon.IataCode &&
            x.DepartureAirportIcaoCode == _incheon.IcaoCode &&
            x.DepartureAirportName == _incheon.Name &&
            x.DepartureAirportTimeZoneId == _incheon.TimeZoneId &&
            x.ArrivalAirportId == request.ArrivalAirportId &&
            x.ArrivalAirportIataCode == _schipol.IataCode &&
            x.ArrivalAirportIcaoCode == _schipol.IcaoCode &&
            x.ArrivalAirportName == _schipol.Name &&
            x.ArrivalAirportTimeZoneId == _schipol.TimeZoneId &&
            x.DepartureLocalTime == request.DepartureLocalTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) &&
            x.ArrivalLocalTime == request.ArrivalLocalTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) &&
            x.Duration == duration &&
            x.EconomyPrice == request.EconomyPrice &&
            x.BusinessPrice == request.BusinessPrice &&
            x.AircraftId == request.AircraftId &&
            x.AircraftEquipmentCode == _aircraft.EquipmentCode &&
            x.AircraftTailNumber == _aircraft.TailNumber);
    }

    [Fact, TestPriority(1)]
    public async Task GetById_Should_ReturnNotFound_WhenFlightDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var error = $"Flight with ID {id} not found";

        // Act
        var uri = new Uri($"flights/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);

        // Assert
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact, TestPriority(2)]
    public async Task GetById_Should_ReturnOk_WhenFlightExists()
    {
        // Arrange
        ArgumentNullException.ThrowIfNull(_dto);
        var id = _dto.Id;

        // Act
        var uri = new Uri($"flights/{id}", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);
        var flightDto = await response.Content.ReadFromJsonAsync<FlightDto>(TestContext.Current.CancellationToken);

        // Assert
        flightDto.Should().BeEquivalentTo(_dto);
    }
}
