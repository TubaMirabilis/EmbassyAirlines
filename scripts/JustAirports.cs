#:project ../src/Shared
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Contracts;
using SmokeTests;

var baseAddress = args.Length > 0 ? args[0] : "http://localhost:3000/";
using var client = new HttpClient
{
    BaseAddress = new Uri(baseAddress)
};
var req1 = new CreateOrUpdateAirportDto("RKSI", "ICN", "Incheon International Airport", "Asia/Seoul");
Console.WriteLine($"Attempting to create resource for {req1.Name} ({req1.IataCode})");
var res1 = await client.PostAsJsonAsync("airports", req1, SmokeTestJsonContext.Default.CreateOrUpdateAirportDto);
res1.EnsureSuccessStatusCode();
var stream1 = await res1.Content.ReadAsStreamAsync();
var airport1 = await JsonSerializer.DeserializeAsync<AirportDto>(stream1, SmokeTestJsonContext.Default.AirportDto);
if (airport1 is null)
{
    throw new JsonException("Deserialization returned null");
}
Console.WriteLine($"Created airport with ID: {airport1.Id}");
var req2 = new CreateOrUpdateAirportDto("EHAM", "AMS", "Schiphol Airport", "Europe/Amsterdam");
Console.WriteLine($"Attempting to create resource for {req2.Name} ({req2.IataCode})");
var res2 = await client.PostAsJsonAsync("airports", req2, SmokeTestJsonContext.Default.CreateOrUpdateAirportDto);
res2.EnsureSuccessStatusCode();
var stream2 = await res2.Content.ReadAsStreamAsync();
var airport2 = await JsonSerializer.DeserializeAsync<AirportDto>(stream2, SmokeTestJsonContext.Default.AirportDto);
if (airport2 is null)
{
    throw new JsonException("Deserialization returned null");
}
Console.WriteLine($"Created airport with ID: {airport2.Id}");

namespace SmokeTests
{
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(AirportDto))]
    [JsonSerializable(typeof(CreateOrUpdateAirportDto))]
    internal sealed partial class SmokeTestJsonContext : JsonSerializerContext
    {
    }
}
