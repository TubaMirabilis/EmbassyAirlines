namespace Flights.Api.Domain.Seats;

public interface ISeatService
{
    IEnumerable<Seat> CreateSeats(string equipmentType, decimal economyPrice, decimal businessPrice);
}
