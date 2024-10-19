using Flights.Api.Entities;

namespace Flights.Api.Services;

public interface ISeatService
{
    IEnumerable<Seat> CreateSeats(string equipmentType, decimal economyPrice, decimal businessPrice);
}
