using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Shared.Contracts;

namespace SmokeTests;

internal static class AirportsSmokeTestActions
{
    public static async Task<AirportDto> AttemptPostAsync(HttpClient client, CreateOrUpdateAirportDto req)
    {
        Console.WriteLine($"Attempting to create resource for {req.Name} ({req.IataCode})");
        var startTime = Stopwatch.GetTimestamp();
        var res = await client.PostAsJsonAsync("airports", req);
        res.EnsureSuccessStatusCode();
        var stream = await res.Content.ReadAsStreamAsync();
        var airport = await JsonSerializer.DeserializeAsync<AirportDto>(stream, JsonSerializerOptions.Web);
        if (airport is null)
        {
            throw new JsonException("Deserialization returned null");
        }
        var diff = Stopwatch.GetElapsedTime(startTime);
        Console.WriteLine($"Operation completed in {diff.TotalMilliseconds:F0} milliseconds");
        Console.WriteLine($"Created airport with ID: {airport.Id}");
        return airport;
    }
}
