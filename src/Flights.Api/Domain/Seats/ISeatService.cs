namespace Flights.Api.Domain.Seats;

internal interface ISeatService
{
    IEnumerable<Seat> CreateSeats(string equipmentType, decimal economyPrice, decimal businessPrice);
}
