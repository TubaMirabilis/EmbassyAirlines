using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shared.Contracts;

namespace SmokeTests;

internal static class FlightsSmokeTestActions
{
    private const int MaxRetryAttempts = 180;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    public static async Task<FlightDto> AttemptPostAsync(HttpClient client, ScheduleFlightDto req, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Attempting to schedule flight between airports {req.DepartureAirportId} and {req.ArrivalAirportId}");
        var startTime = Stopwatch.GetTimestamp();
        using var res = await PostWithRetryAsync(client, req, cancellationToken);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync(cancellationToken);
        var flight = await JsonSerializer.DeserializeAsync<FlightDto>(stream, JsonSerializerOptions.Web, cancellationToken);
        if (flight is null)
        {
            throw new JsonException("Deserialization returned null");
        }
        var diff = Stopwatch.GetElapsedTime(startTime);
        Console.WriteLine($"Operation completed in {diff.TotalMilliseconds:F0} milliseconds");
        Console.WriteLine($"Scheduled flight with ID: {flight.Id}");
        return flight;
    }
    public static async Task AttemptPatchMilestoneAsync(HttpClient client,
                                                        Guid flightId,
                                                        string milestone,
                                                        CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Attempting to record flight milestone {milestone} for flight {flightId}");
        var dto = new RecordFlightMilestoneDto(milestone, DateTimeOffset.UtcNow);
        var startTime = Stopwatch.GetTimestamp();
        using var res = await client.PatchAsJsonAsync($"flights/{flightId}/milestones", dto, cancellationToken);
        res.EnsureSuccessStatusCode();
        var diff = Stopwatch.GetElapsedTime(startTime);
        Console.WriteLine($"Operation completed in {diff.TotalMilliseconds:F0} milliseconds");
        Console.WriteLine($"Recorded milestone {milestone} for flight {flightId}");
    }
    private static async Task<HttpResponseMessage> PostWithRetryAsync(HttpClient client, ScheduleFlightDto req, CancellationToken cancellationToken)
    {
        foreach (var attempt in Enumerable.Range(0, MaxRetryAttempts + 1))
        {
            var response = await client.PostAsJsonAsync(
                "flights", req, cancellationToken);
            if (response.StatusCode != HttpStatusCode.NotFound ||
                attempt == MaxRetryAttempts)
            {
                if (response.StatusCode == HttpStatusCode.NotFound &&
                    attempt == MaxRetryAttempts)
                {
                    Console.WriteLine(
                        $"Flight scheduling failed after {attempt + 1} attempts. " +
                        $"Last status: {response.StatusCode}");
                }
                return response;
            }
            response.Dispose();
            await Task.Delay(RetryDelay, cancellationToken);
        }
        throw new InvalidOperationException("Retry loop exited unexpectedly.");
    }
}
