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
        sb.AppendLine("Passengers:");
        foreach (var (passenger, seat) in booking.Passengers.Values)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"- {passenger.FirstName} {passenger.LastName} in seat {seat.SeatNumber}");
        }
        return sb.ToString();
    }
}
