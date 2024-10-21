using System.Globalization;
using System.Text.Json;
using Flights.Api.Database;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using TechTalk.SpecFlow;

namespace Flights.Api.AcceptanceTests.StepDefinitions;

[Binding]
public sealed class GetSeatsForFlightSteps
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private readonly IServiceScope _scope;
    private readonly JsonSerializerOptions _options;
    public GetSeatsForFlightSteps(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        _options = _scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
    }
    [When(@"I get the seats for flight (.*)")]
    public async Task WhenIGetTheSeatsForFlight(string flightNumber)
    {
        var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var flight = await dbContext.Flights.SingleAsync(f => f.FlightNumber == flightNumber);
        var id = flight.Id;
        var url = $"/flights/{id}/seats";
        _response = await _client.GetAsync(url);
    }
    [Then(@"a concatenation of the following seat groups is returned:")]
    public async Task ThenAConcatenationOfTheFollowingSeatGroupsIsReturned(Table table)
    {
        ArgumentNullException.ThrowIfNull(_response);
        _response.EnsureSuccessStatusCode();
        var content = await _response.Content.ReadAsStreamAsync();
        var seats = await JsonSerializer.DeserializeAsync<IEnumerable<SeatDto>>(content, _options);
        ArgumentNullException.ThrowIfNull(seats);
        foreach (var row in table.Rows)
        {
            var seatType = row["SeatType"];
            var count = int.Parse(row["Count"], CultureInfo.InvariantCulture);
            var price = decimal.Parse(row["Price"], CultureInfo.InvariantCulture);
            var available = int.Parse(row["Available"], CultureInfo.InvariantCulture);
            var group = seats.Where(s => s.SeatType == seatType).ToList();
            group.Should().HaveCount(count);
            group.Should().OnlyContain(s => s.Price == price);
            group.Count(s => s.IsAvailable).Should().Be(available);
        }
    }
}
