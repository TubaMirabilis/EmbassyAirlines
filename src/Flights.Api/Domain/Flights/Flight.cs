using System.Globalization;

namespace Flights.Api.Domain.Flights;

public sealed class Flight
{
    private Flight(FlightCreationArgs args)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Number = args.Number;
        NumberIataFormat = args.NumberIataFormat;
        NumberIcaoFormat = args.NumberIcaoFormat;
        DepartureTimeUtc = args.DepartureTimeUtc;
        ArrivalTimeUtc = args.ArrivalTimeUtc;
        Aircraft = args.Aircraft;
        Status = args.Status;
        DepartureGate = args.DepartureGate;
        ArrivalGate = args.ArrivalGate;
        DepartureTerminal = args.DepartureTerminal;
        ArrivalTerminal = args.ArrivalTerminal;
        DepartureAirport = args.DepartureAirport;
        ArrivalAirport = args.ArrivalAirport;
        AdultMen = args.AdultMen;
        AdultWomen = args.AdultWomen;
        Children = args.Children;
        CheckedBags = args.CheckedBags;
        Notes = args.Notes;
    }
#pragma warning disable CS8618
    private Flight()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string Number { get; set; }
    public string NumberIataFormat { get; set; }
    public string NumberIcaoFormat { get; set; }
    public DateTime DepartureTimeUtc { get; set; }
    public DateTime ArrivalTimeUtc { get; set; }
    public Aircraft Aircraft { get; set; }
    public FlightStatus Status { get; set; }
    public string DepartureGate { get; set; }
    public string ArrivalGate { get; set; }
    public string DepartureTerminal { get; set; }
    public string ArrivalTerminal { get; set; }
    public Airport DepartureAirport { get; set; }
    public Airport ArrivalAirport { get; set; }
    public double Distance => CalculateGreatCircleDistance(DepartureAirport, ArrivalAirport);
    public short AdultMen { get; set; }
    public short AdultWomen { get; set; }
    public short Children { get; set; }
    public short CheckedBags { get; set; }
    public string Notes { get; set; }
    public string? DepartureTaf { get; set; }
    public string? ArrivalTaf { get; set; }
    public string? DepartureMetar { get; set; }
    public string? ArrivalMetar { get; set; }
    public DateTime? ActualDepartureTimeUtc { get; set; }
    public DateTime? ActualArrivalTimeUtc { get; set; }
    public int TotalPassengers
        => AdultMen + AdultWomen + Children;
    public string Duration
        => (ArrivalTimeUtc - DepartureTimeUtc).ToString("hh\\:mm", CultureInfo.InvariantCulture);
    public DateTime DepartureTimeLocal
        => TimeZoneInfo.ConvertTimeFromUtc(DepartureTimeUtc, TimeZoneInfo.FindSystemTimeZoneById(DepartureAirport.TimeZoneId));
    public DateTime ArrivalTimeLocal
        => TimeZoneInfo.ConvertTimeFromUtc(ArrivalTimeUtc, TimeZoneInfo.FindSystemTimeZoneById(ArrivalAirport.TimeZoneId));
    public static Flight Create(FlightCreationArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return new(args);
    }

    private static double CalculateGreatCircleDistance(Airport origin, Airport destination)
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
