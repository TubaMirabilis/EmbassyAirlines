using System.Text.Json;

namespace Deployment;

internal static class SmokeTester
{
    public static async Task<bool> TestLambdaProxyAsync(string url, string body)
    {
        Console.WriteLine($"Performing smoke test on {url}");
        var uri = new Uri(url);
        using var httpClient = new HttpClient();
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        try
        {
            var response = await httpClient.PostAsync(uri, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Smoke test failed with status code: {response.StatusCode}");
                Console.WriteLine($"Response body: {responseBody}");
                return false;
            }
            var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("statusCode", out var statusCodeProp) && statusCodeProp.ValueKind == JsonValueKind.Number)
            {
                var statusCode = statusCodeProp.GetInt32();
                if (statusCode >= 400)
                {
                    Console.WriteLine($"Smoke test failed with statusCode: {statusCode}");
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Smoke test response does not contain a valid statusCode.");
                return false;
            }
            Console.WriteLine("Smoke test succeeded.");
            Console.WriteLine($"Response body: {responseBody}");
            return true;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP request failed: {ex.Message}");
            return false;
        }
    }
}
