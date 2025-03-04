using Flights.Api.Domain.Passengers;
using Shared.Contracts;

namespace Flights.Api.Extensions;

internal static class PassengerExtensions
{
    public static PassengerDto ToDto(this Passenger passenger) => new(passenger.FirstName, passenger.LastName);
}
