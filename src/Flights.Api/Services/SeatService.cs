using Flights.Api.Entities;

namespace Flights.Api.Services;

public sealed class SeatService : ISeatService
{
    public IEnumerable<Seat> CreateSeats(string equipmentType)
    {
        return equipmentType switch
        {
            "B78X" => CreateSeatsForB78X(),
            _ => throw new ArgumentException("Invalid equipment type")
        };
    }
    private static List<Seat> CreateSeatsForB78X()
    {
        var seats = new List<Seat>();
        char[] config1 = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J'];
        char[] config2 = ['A', 'B', 'C', 'D', 'F', 'G', 'H', 'J'];
        char[] config3 = ['A', 'B', 'D', 'E', 'F', 'G', 'J'];
        for (var i = 1; i <= 18; i++)
        {
            if (i % 2 != 0)
            {
                seats.Add(Seat.Create($"{i}A", SeatType.Business));
                seats.Add(Seat.Create($"{i}K", SeatType.Business));
            }
            if (i % 2 == 0)
            {
                seats.Add(Seat.Create($"{i}D", SeatType.Business));
                seats.Add(Seat.Create($"{i}F", SeatType.Business));
            }
        }
        for (var i = 19; i <= 52; i++)
        {
            if (i < 50)
            {
                seats.AddRange(CreateRowOfSeats(i, SeatType.Economy, config1));
            }
            if (i == 50)
            {
                seats.AddRange(CreateRowOfSeats(i, SeatType.Economy, config2));
            }
            if (i > 50)
            {
                seats.AddRange(CreateRowOfSeats(i, SeatType.Economy, config3));
            }
        }
        return seats;
    }
    private static IEnumerable<Seat> CreateRowOfSeats(int rowNumber, SeatType seatType, IEnumerable<char> seatLetters)
    {
        foreach (var seatLetter in seatLetters)
        {
            yield return Seat.Create($"{rowNumber}{seatLetter}", seatType);
        }
    }
}
