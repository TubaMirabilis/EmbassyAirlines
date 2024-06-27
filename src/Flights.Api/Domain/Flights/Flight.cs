using System.Globalization;

namespace Flights.Api.Domain.Flights;

public sealed class Flight
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required string Number { get; set; }
    public required string NumberIataFormat { get; set; }
    public required string NumberIcaoFormat { get; set; }
    public required DateTime DepartureTimeUtc { get; set; }
    public required DateTime ArrivalTimeUtc { get; set; }
    public required Aircraft Aircraft { get; set; }
    public required FlightStatus Status { get; set; }
    public required string DepartureGate { get; set; }
    public required string ArrivalGate { get; set; }
    public required string DepartureTerminal { get; set; }
    public required string ArrivalTerminal { get; set; }
    public required Airport DepartureAirport { get; set; }
    public required Airport ArrivalAirport { get; set; }
    public int Distance => CalculateGreatCircleDistance(DepartureAirport, ArrivalAirport);
    public required short AdultMen { get; set; }
    public required short AdultWomen { get; set; }
    public required short Children { get; set; }
    public required short CheckedBags { get; set; }
    public required string Notes { get; set; }
    public string? DepartureTaf { get; set; }
    public string? ArrivalTaf { get; set; }
    public string? DepartureMetar { get; set; }
    public string? ArrivalMetar { get; set; }
    public DateTime? ActualDepartureTimeUtc { get; set; }
    public DateTime? ActualArrivalTimeUtc { get; set; }
    public short TotalPassengers
        => (short)(AdultMen + AdultWomen + Children);
    public string Duration
        => (ArrivalTimeUtc - DepartureTimeUtc).ToString("hh\\:mm", CultureInfo.InvariantCulture);
    public DateTime DepartureTimeLocal
        => TimeZoneInfo.ConvertTimeFromUtc(DepartureTimeUtc, TimeZoneInfo.FindSystemTimeZoneById(DepartureAirport.TimeZoneId));
    public DateTime ArrivalTimeLocal
        => TimeZoneInfo.ConvertTimeFromUtc(ArrivalTimeUtc, TimeZoneInfo.FindSystemTimeZoneById(ArrivalAirport.TimeZoneId));
    public int CalculateGreatCircleDistance(Airport origin, Airport destination)
    {
        double originLatitudeRadians = origin.Latitude * (Math.PI / 180);
        double originLongitudeRadians = origin.Longitude * (Math.PI / 180);
        double destinationLatitudeRadians = destination.Latitude * (Math.PI / 180);
        double destinationLongitudeRadians = destination.Longitude * (Math.PI / 180);
        double dLat = destinationLatitudeRadians - originLatitudeRadians;
        double dLon = destinationLongitudeRadians - originLongitudeRadians;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(originLatitudeRadians) * Math.Cos(destinationLatitudeRadians) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        const double earthRadiusNauticalMiles = 3440.065;
        double distance = earthRadiusNauticalMiles * c;
        return Math.Round(distance);
    }
}
