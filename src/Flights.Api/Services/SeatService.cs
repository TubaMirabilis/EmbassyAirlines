using Flights.Api.Entities;

namespace Flights.Api.Services;

public sealed class SeatService : ISeatService
{
    public IEnumerable<Seat> CreateSeats(string equipmentType, decimal economyPrice, decimal businessPrice)
    {
        return equipmentType switch
        {
            "B78X" => CreateSeatsForB78X(economyPrice, businessPrice),
            _ => throw new ArgumentException("Invalid equipment type")
        };
    }
    private static List<Seat> CreateSeatsForB78X(decimal economyPrice, decimal businessPrice)
    {
        var seats = new List<Seat>();
        char[] config1 = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J'];
        char[] config2 = ['A', 'B', 'C', 'D', 'F', 'G', 'H', 'J'];
        char[] config3 = ['A', 'B', 'D', 'E', 'F', 'G', 'J'];
        for (var i = 1; i <= 18; i++)
        {
            if (i % 2 != 0)
            {
                seats.Add(Seat.Create($"{i}A", SeatType.Business, businessPrice));
                seats.Add(Seat.Create($"{i}K", SeatType.Business, businessPrice));
            }
            if (i % 2 == 0)
            {
                seats.Add(Seat.Create($"{i}D", SeatType.Business, businessPrice));
                seats.Add(Seat.Create($"{i}F", SeatType.Business, businessPrice));
            }
        }
        for (var i = 19; i <= 52; i++)
        {
            if (i < 50)
            {
                seats.AddRange(CreateRowOfSeats(i, SeatType.Economy, config1, economyPrice, businessPrice));
            }
            if (i == 50)
            {
                seats.AddRange(CreateRowOfSeats(i, SeatType.Economy, config2, economyPrice, businessPrice));
            }
            if (i > 50)
            {
                seats.AddRange(CreateRowOfSeats(i, SeatType.Economy, config3, economyPrice, businessPrice));
            }
        }
        return seats;
    }
    private static IEnumerable<Seat> CreateRowOfSeats(int rowNumber, SeatType seatType,
        IEnumerable<char> seatLetters, decimal economyPrice, decimal businessPrice)
    {
        foreach (var seatLetter in seatLetters)
        {
            if (seatType == SeatType.Economy)
            {
                yield return Seat.Create($"{rowNumber}{seatLetter}", seatType, economyPrice);
            }
            if (seatType == SeatType.Business)
            {
                yield return Seat.Create($"{rowNumber}{seatLetter}", seatType, businessPrice);
            }
        }
    }
}
