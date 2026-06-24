using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shared.Contracts;

namespace SmokeTests;

internal static class FlightsSmokeTestActions
{
    public static async Task AttemptPostAsync(HttpClient client, ScheduleFlightDto req)
    {
        Console.WriteLine($"Attempting to schedule flight between airports {req.DepartureAirportId} and {req.ArrivalAirportId}");
        var res = await client.PostAsJsonAsync("flights", req);
        res.EnsureSuccessStatusCode();
        var stream = await res.Content.ReadAsStreamAsync();
        var flight = await JsonSerializer.DeserializeAsync<FlightDto>(stream);
        if (flight is null)
        {
            throw new JsonException("Deserialization returned null");
        }
        Console.WriteLine($"Scheduled flight with ID: {flight.Id}");
    }
    public static async Task ReadyAsync(HttpClient client)
    {
        var uri = new Uri("flights", UriKind.Relative);
        var response = await client.GetAsync(uri);
        if (response.StatusCode is not HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Request to {uri} returned status code {response.StatusCode}. The Flights service may not be ready yet.");
        }
        await AsyncTestHelpers.Eventually(async () =>
        {
            var summary = await client.GetFromJsonAsync<FlightsSummaryDto>("flights/summary");
            return summary is not null
                && summary.AirportCount == 2
                && summary.AircraftCount == 1;
        }, timeout: TimeSpan.FromSeconds(60));
    }
}
