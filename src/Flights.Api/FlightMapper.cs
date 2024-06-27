using Flights.Api.Contracts;
using Flights.Api.Domain.Flights;
using Riok.Mapperly.Abstractions;

namespace Flights.Api;

[Mapper]
public partial class FlightMapper
{
    public partial FlightResponse MapFlightToFlightResponse(Flight flight);
}
