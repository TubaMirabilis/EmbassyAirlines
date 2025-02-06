using System.Globalization;
using System.Text;
using Shared.Contracts;

namespace Emails.Lambda;

public static class BookingDtoExtensions
{
    public static string GetSummary(this BookingDto booking)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Flight: {booking.FlightNumber}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"{booking.DepartureIata} {booking.Departure} to {booking.DestinationIata} {booking.Destination}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Departure: {booking.DepartureTime}");
        sb.AppendLine();
        sb.AppendLine("Passengers:");
        foreach (var (passenger, seat) in booking.Passengers.Values)
        {
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"- {passenger.FirstName} {passenger.LastName} in seat {seat.SeatNumber}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Price: {seat.Price:C2}");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
