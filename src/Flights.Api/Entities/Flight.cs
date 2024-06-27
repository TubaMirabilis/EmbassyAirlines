using System.Globalization;
using Flights.Api.Enums;

namespace Flights.Api.Entities;

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
    public string DepartureTimeZoneId => DepartureAirport.TimeZoneId;
    public string ArrivalTimeZoneId => ArrivalAirport.TimeZoneId;
    public required string AircraftTypeDesignator { get; set; }
    public required string AircraftRegistration { get; set; }
    public required FlightStatus Status { get; set; }
    public required string DepartureGate { get; set; }
    public required string ArrivalGate { get; set; }
    public required string DepartureTerminal { get; set; }
    public required string ArrivalTerminal { get; set; }
    public required string DepartureAirportFullName { get; set; }
    public required string ArrivalAirportFullName { get; set; }
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
