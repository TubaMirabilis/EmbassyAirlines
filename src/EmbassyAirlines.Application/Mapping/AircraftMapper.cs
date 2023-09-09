using EmbassyAirlines.Application.Dtos;
using EmbassyAirlines.Domain;
using Riok.Mapperly.Abstractions;

namespace EmbassyAirlines.Application.Mapping;

[Mapper]
public partial class AircraftMapper
{
    public partial AircraftDto MapAircraftToAircraftDto(Aircraft aircraft);
    public Aircraft MapNewAircraftDtoToAircraft(NewAircraftDto dto)
    {
        var aircraft = NewAircraftDtoToAircraft(dto);
        aircraft.Id = Guid.NewGuid();
        aircraft.CreatedAt = DateTime.UtcNow;
        aircraft.UpdatedAt = DateTime.UtcNow;
        return aircraft;
    }
    private partial Aircraft NewAircraftDtoToAircraft(NewAircraftDto dto);
}