using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Shared.Extensions;

public static class HttpResponseMessageExtensions
{
    public static async Task<ProblemDetails> ReadProblemDetailsAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStreamAsync();
        var pd = await JsonSerializer.DeserializeAsync<ProblemDetails>(content);
        ArgumentNullException.ThrowIfNull(pd);
        return pd;
    }
}
