﻿using NodaTime;

namespace Flights.Api;

internal sealed class Flight
{
    private Flight(FlightCreationArgs args)
    {
        Id = Guid.NewGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();
        FlightNumberIata = args.FlightNumberIata;
        FlightNumberIcao = args.FlightNumberIcao;
        DepartureLocalTime = args.DepartureLocalTime;
        ArrivalLocalTime = args.ArrivalLocalTime;
        DepartureAirport = args.DepartureAirport;
        ArrivalAirport = args.ArrivalAirport;
        Aircraft = args.Aircraft;
        EconomyPrice = args.EconomyPrice;
        BusinessPrice = args.BusinessPrice;
    }
#pragma warning disable CS8618
    private Flight()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public Instant CreatedAt { get; init; }
    public string FlightNumberIata { get; private set; }
    public string FlightNumberIcao { get; private set; }
    public LocalDateTime DepartureLocalTime { get; private set; }
    public LocalDateTime ArrivalLocalTime { get; private set; }
    public ZonedDateTime DepartureZonedTime => DepartureLocalTime.InZoneStrictly(DepartureAirport.TimeZone);
    public ZonedDateTime ArrivalZonedTime => ArrivalLocalTime.InZoneStrictly(ArrivalAirport.TimeZone);
    public Airport DepartureAirport { get; init; }
    public Airport ArrivalAirport { get; init; }
    public Aircraft Aircraft { get; private set; }
    public Money EconomyPrice { get; private set; }
    public Money BusinessPrice { get; private set; }
    public Instant DepartureInstant => DepartureZonedTime.ToInstant();
    public Instant ArrivalInstant => ArrivalZonedTime.ToInstant();
    public static Flight Create(FlightCreationArgs args) => new(args);
    public void AssignAircraft(Aircraft aircraft) => Aircraft = aircraft;
    public void AdjustPricing(Money economyPrice, Money businessPrice)
    {
        EconomyPrice = economyPrice;
        BusinessPrice = businessPrice;
    }
    public void Reschedule(LocalDateTime newDepartureLocalTime, LocalDateTime newArrivalLocalTime)
    {
        DepartureLocalTime = newDepartureLocalTime;
        ArrivalLocalTime = newArrivalLocalTime;
    }
}
