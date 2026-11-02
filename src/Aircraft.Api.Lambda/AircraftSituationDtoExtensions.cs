using Aircraft.Core.Models;
using Shared.Contracts;

namespace Aircraft.Api.Lambda;

internal static class AircraftSituationDtoExtensions
{
    public static AircraftSituation ToDomain(
        this AircraftSituationDto dto,
        DateTimeOffset effectiveAt)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!AircraftStatusNames.TryParse(dto.Status, out var status))
        {
            throw new ArgumentException(
                $"Invalid aircraft status: {dto.Status}",
                nameof(dto));
        }

        return status switch
        {
            Status.Parked => AircraftSituation.Parked(
                dto.AirportIcaoCode,
                effectiveAt),

            Status.EnRoute => AircraftSituation.EnRoute(
                dto.AirportIcaoCode,
                effectiveAt),

            _ => throw new ArgumentOutOfRangeException(
                nameof(dto),
                $"Unsupported aircraft status: {dto.Status}")
        };
    }
    public static AircraftSituationDto ToDto(
        this AircraftSituation situation)
    {
        var airportIcaoCode = situation.Status switch
        {
            Status.Parked => situation.ParkedAt!,
            Status.EnRoute => situation.EnRouteTo!,
            _ => throw new InvalidOperationException(
                "Unsupported aircraft situation.")
        };

        return new AircraftSituationDto(
            situation.Status.ToString(),
            airportIcaoCode);
    }
}
