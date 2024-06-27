using System.Globalization;
using Flights.Api.Enums;

namespace Flights.Api.Entities.Flights;

public sealed record FlightCreationArgs
{
    public required string Number { get; init; }
    public required string NumberIataFormat { get; init; }
    public required string NumberIcaoFormat { get; init; }
    public required DateTime DepartureTimeUtc { get; init; }
    public required DateTime ArrivalTimeUtc { get; init; }
    public required string AircraftTypeDesignator { get; set; }
    public required string AircraftRegistration { get; set; }
    public required FlightStatus Status { get; set; }
    public required string DepartureGate { get; set; }
    public required string ArrivalGate { get; set; }
    public required string DepartureTerminal { get; set; }
    public required string ArrivalTerminal { get; set; }
    public string DepartureAirportFullName => DepartureAirport.FullName;
    public string ArrivalAirportFullName => ArrivalAirport.FullName;
    public string DepartureAirportIata => DepartureAirport.Iata;
    public string ArrivalAirportIata => ArrivalAirport.Iata;
    public string DepartureAirportIcao => DepartureAirport.Icao;
    public string ArrivalAirportIcao => ArrivalAirport.Icao;
    public required Airport DepartureAirport { get; set; }
    public required Airport ArrivalAirport { get; set; }
    public required short Distance { get; set; }
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
        => TimeZoneInfo.ConvertTimeFromUtc(DepartureTimeUtc, TimeZoneInfo.FindSystemTimeZoneById(DepartureTimeZoneId));
    public DateTime ArrivalTimeLocal
        => TimeZoneInfo.ConvertTimeFromUtc(ArrivalTimeUtc, TimeZoneInfo.FindSystemTimeZoneById(ArrivalTimeZoneId));
    public string DepartureTimeLocalString
        => DepartureTimeLocal.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    public string ArrivalTimeLocalString
        => ArrivalTimeLocal.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
}
