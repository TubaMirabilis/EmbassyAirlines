using Fleet.Api.Contracts;
using Fleet.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace Fleet.Api;

[Mapper]
public partial class AircraftMapper
{
    public partial AircraftResponse MapAircraftToAircraftResponse(Aircraft aircraft);
    public Aircraft MapAddAircraftRequestToAircraft(AddAircraftRequest request)
    {
        var aircraft = AddAircraftRequestToAircraft(request);
        aircraft.Id = Guid.NewGuid();
        aircraft.CreatedAt = DateTime.UtcNow;
        aircraft.UpdatedAt = DateTime.UtcNow;
        return aircraft;
    }
    private partial Aircraft AddAircraftRequestToAircraft(AddAircraftRequest request);
}
