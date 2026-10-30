using System.Collections.ObjectModel;

namespace Flights.Core.Models;

public static class FlightStatusTransitions
{
    private static readonly ReadOnlyDictionary<FlightStatus, ISet<FlightStatus>> Allowed = new Dictionary<FlightStatus, ISet<FlightStatus>>
    {
        { FlightStatus.Scheduled, new HashSet<FlightStatus> { FlightStatus.EnRoute, FlightStatus.Cancelled, FlightStatus.Delayed } },
        { FlightStatus.EnRoute, new HashSet<FlightStatus> { FlightStatus.Arrived } },
        { FlightStatus.Delayed, new HashSet<FlightStatus> { FlightStatus.EnRoute, FlightStatus.Cancelled } },
        { FlightStatus.Arrived, new HashSet<FlightStatus>() },
        { FlightStatus.Cancelled, new HashSet<FlightStatus>() }
    }.AsReadOnly();
    public static bool CanTransition(FlightStatus from, FlightStatus to)
    {
        if (from == to)
        {
            return false;
        }
        return Allowed.TryGetValue(from, out var allowedStatuses) && allowedStatuses.Contains(to);
    }
}
