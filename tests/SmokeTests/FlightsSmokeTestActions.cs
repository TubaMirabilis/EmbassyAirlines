using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;
using Polly.Retry;
using Shared.Contracts;

namespace SmokeTests;

internal static class FlightsSmokeTestActions
{
    private static readonly ResiliencePipeline<HttpResponseMessage> RetryNotFoundPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>().AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 5,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>().HandleResult(r => r.StatusCode == HttpStatusCode.NotFound)
    }).Build();
    public static async Task AttemptPostAsync(HttpClient client, ScheduleFlightDto req)
    {
        Console.WriteLine($"Attempting to schedule flight between airports {req.DepartureAirportId} and {req.ArrivalAirportId}");
        var startTime = Stopwatch.GetTimestamp();
        var res = await RetryNotFoundPipeline.ExecuteAsync(async cancellationToken => await client.PostAsJsonAsync("flights", req, cancellationToken));
        res.EnsureSuccessStatusCode();
        var stream = await res.Content.ReadAsStreamAsync();
        var flight = await JsonSerializer.DeserializeAsync<FlightDto>(stream);
        if (flight is null)
        {
            throw new JsonException("Deserialization returned null");
        }
        var diff = Stopwatch.GetElapsedTime(startTime);
        Console.WriteLine($"Operation completed in {diff.TotalMilliseconds:F0} milliseconds");
        Console.WriteLine($"Scheduled flight with ID: {flight.Id}");
    }
}
