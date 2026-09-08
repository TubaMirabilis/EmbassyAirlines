using System.Text.Json;

namespace Shared;

public static class OutboxJson
{
    private static readonly JsonSerializerOptions s_serializerOptions =
        new(JsonSerializerDefaults.Web);
    public static T Deserialize<T>(string content)
        => JsonSerializer.Deserialize<T>(content, s_serializerOptions)
           ?? throw new JsonException(
               $"Outbox payload for {typeof(T).Name} deserialised to null.");
}
