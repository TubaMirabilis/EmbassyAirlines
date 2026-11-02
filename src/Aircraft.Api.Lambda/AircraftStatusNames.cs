using Aircraft.Core.Models;

namespace Aircraft.Api.Lambda;

internal static class AircraftStatusNames
{
    private static readonly Dictionary<string, Status> ByName =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(Status.Parked)] = Status.Parked,
            [nameof(Status.EnRoute)] = Status.EnRoute,
        };

    public static bool TryParse(string? value, out Status status) =>
        ByName.TryGetValue(value ?? string.Empty, out status);
}
