namespace Flights.Api.Domain.Seats;

internal sealed class SeatService : ISeatService
{
    public IEnumerable<Seat> CreateSeats(string equipmentType, decimal economyPrice, decimal businessPrice)
        => equipmentType switch
        {
            "B78X" => CreateSeatsForB78X(economyPrice, businessPrice),
            _ => throw new ArgumentException("Invalid equipment type")
        };
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
                seats.Add(Seat.Create(SeatType.Business, $"{i}A", businessPrice));
                seats.Add(Seat.Create(SeatType.Business, $"{i}K", businessPrice));
            }
            if (i % 2 == 0)
            {
                seats.Add(Seat.Create(SeatType.Business, $"{i}D", businessPrice));
                seats.Add(Seat.Create(SeatType.Business, $"{i}F", businessPrice));
            }
        }
        for (var i = 19; i <= 52; i++)
        {
            if (i < 50)
            {
                seats.AddRange(CreateRowOfSeats(i, SeatType.Economy, config1, economyPrice));
            }
            if (i == 50)
            {
                seats.AddRange(CreateRowOfSeats(i, SeatType.Economy, config2, economyPrice));
            }
            if (i > 50)
            {
                seats.AddRange(CreateRowOfSeats(i, SeatType.Economy, config3, economyPrice));
            }
        }
        return seats;
    }
    private static IEnumerable<Seat> CreateRowOfSeats(int rowNumber, SeatType seatType, IEnumerable<char> seatLetters, decimal price)
    {
        foreach (var seatLetter in seatLetters)
        {
            yield return Seat.Create(seatType, $"{rowNumber}{seatLetter}", price);
        }
    }
}
