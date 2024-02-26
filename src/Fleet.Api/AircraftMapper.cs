using Fleet.Api.Contracts;
using Fleet.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace Fleet.Api;

[Mapper]
public partial class AircraftMapper
{
    public partial AircraftResponse MapAircraftToAircraftResponse(Aircraft aircraft);
}
