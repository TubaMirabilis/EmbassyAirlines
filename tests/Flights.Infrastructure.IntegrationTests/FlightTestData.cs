using Flights.Core.Models;
using NodaTime;

namespace Flights.Infrastructure.IntegrationTests;

internal static class FlightTestData
{
    public static Flight Create(
        Aircraft aircraft,
        Airport departureAirport,
        Airport arrivalAirport,
        Instant now)
    {
        var departureInstant = now + Duration.FromHours(2);
        var arrivalInstant = departureInstant
            + Duration.FromHours(10);

        var schedule = new FlightSchedule(
            new FlightScheduleCreationArgs
            {
                DepartureAirport = departureAirport,
                ArrivalAirport = arrivalAirport,

                DepartureLocalTime = departureInstant
                    .InZone(departureAirport.TimeZone)
                    .LocalDateTime,

                ArrivalLocalTime = arrivalInstant
                    .InZone(arrivalAirport.TimeZone)
                    .LocalDateTime,

                Now = now,

                SchedulingAmbiguityPolicy =
                    SchedulingAmbiguityPolicy.ThrowWhenAmbiguous
            });

        return Flight.Create(new FlightCreationArgs
        {
            Aircraft = aircraft,
            BusinessPrice = new Money(4000),
            EconomyPrice = new Money(400),
            CreatedAt = now,
            FlightNumberIata = "EB1",
            FlightNumberIcao = "EBY1",
            Schedule = schedule,
            OperationType = OperationType.RevenuePassenger
        });
    }
}
