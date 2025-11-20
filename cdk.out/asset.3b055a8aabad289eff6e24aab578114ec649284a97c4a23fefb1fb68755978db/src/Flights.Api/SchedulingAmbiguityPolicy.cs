namespace Flights.Api;

internal enum SchedulingAmbiguityPolicy
{
    ThrowWhenAmbiguous,
    PreferEarlier,
    PreferLater
}
