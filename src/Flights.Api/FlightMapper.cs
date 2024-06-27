using Flights.Api.Contracts;
using Flights.Api.Domain.Flights;
using Riok.Mapperly.Abstractions;

namespace Flights.Api;

[Mapper]
public partial class FlightMapper
{
    public partial FlightResponse MapFlightToFlightResponse(Flight flight);
    public Flight MapAddOrUpdateFlightRequestToFlight(AddOrUpdateFlightRequest request)
    {
        var flight = AddOrUpdateFlightRequestToFlight(request);
        flight.Id = Guid.NewGuid();
        flight.CreatedAt = DateTime.UtcNow;
        flight.UpdatedAt = DateTime.UtcNow;
        return flight;
    }
    private partial Flight AddOrUpdateFlightRequestToFlight(AddOrUpdateFlightRequest request);
}
