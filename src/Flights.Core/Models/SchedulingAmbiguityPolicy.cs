namespace Flights.Core.Models;

public enum SchedulingAmbiguityPolicy
{
    ThrowWhenAmbiguous,
    PreferEarlier,
    PreferLater
}
