using System.Diagnostics;
using System.Net;

namespace SmokeTests;

internal static class ServiceReadiness
{
    public static async Task EnsureReadyAsync(HttpClient client, string endpoint, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Probing {endpoint} service readiness");
        var uri = new Uri(endpoint, UriKind.Relative);
        var startTime = Stopwatch.GetTimestamp();
        var response = await client.GetAsync(uri, cancellationToken);
        if (response.StatusCode is not HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"Request to '{endpoint}' returned status code " +
                $"{(int)response.StatusCode} ({response.StatusCode}). " +
                "The service may not be ready.");
        }
        var diff = Stopwatch.GetElapsedTime(startTime);
        Console.WriteLine($"Operation completed in {diff.TotalMilliseconds:F0} milliseconds");
    }
}
