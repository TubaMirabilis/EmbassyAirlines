using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shared.Contracts;

namespace SmokeTests;

internal static class AirportsSmokeTestActions
{
    public static async Task<AirportDto> AttemptPostAsync(HttpClient client, CreateOrUpdateAirportDto req)
    {
        Console.WriteLine($"Attempting to create resource for {req.Name} ({req.IataCode})");
        var res = await client.PostAsJsonAsync("airports", req);
        res.EnsureSuccessStatusCode();
        var stream = await res.Content.ReadAsStreamAsync();
        var airport = await JsonSerializer.DeserializeAsync<AirportDto>(stream, JsonSerializerOptions.Web);
        if (airport is null)
        {
            throw new JsonException("Deserialization returned null");
        }
        Console.WriteLine($"Created airport with ID: {airport.Id}");
        return airport;
    }
    public static async Task ReadyAsync(HttpClient client)
    {
        var uri = new Uri("airports", UriKind.Relative);
        var response = await client.GetAsync(uri);
        if (response.StatusCode is not HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"Request to {uri} returned status code {response.StatusCode}. The Airports service may not be ready yet.");
        }
    }
}
