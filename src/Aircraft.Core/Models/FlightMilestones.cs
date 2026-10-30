using Shared;

namespace Aircraft.Core.Models;

public sealed record FlightMilestones
{
    public FlightMilestones(DateTimeOffset offBlockAt, DateTimeOffset takeOffAt, DateTimeOffset landingAt, DateTimeOffset onBlockAt)
    {
        Ensure.IsNonDecreasing([offBlockAt, takeOffAt, landingAt, onBlockAt]);
        OffBlockAt = offBlockAt;
        TakeOffAt = takeOffAt;
        LandingAt = landingAt;
        OnBlockAt = onBlockAt;
    }
    private FlightMilestones()
    {
    }
    public DateTimeOffset LandingAt { get; }
    public DateTimeOffset OffBlockAt { get; }
    public DateTimeOffset OnBlockAt { get; }
    public DateTimeOffset TakeOffAt { get; }
}
