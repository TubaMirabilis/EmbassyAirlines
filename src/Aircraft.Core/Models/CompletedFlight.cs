using Shared;

namespace Aircraft.Core.Models;

public sealed class CompletedFlight
{
    private CompletedFlight(CompletedFlightCreationArgs args)
    {
        Ensure.NotEmpty(args.Id);
        Ensure.NotNullOrEmpty(args.DepartureAirportIcao);
        Ensure.NotNullOrEmpty(args.ArrivalAirportIcao);
        Id = args.Id;
        CreatedAt = args.CreatedAt;
        DepartureAirportIcao = args.DepartureAirportIcao;
        ArrivalAirportIcao = args.ArrivalAirportIcao;
        Milestones = args.Milestones;
    }
#pragma warning disable CS8618
    private CompletedFlight()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string DepartureAirportIcao { get; init; }
    public string ArrivalAirportIcao { get; init; }
    public FlightMilestones Milestones { get; init; }
    public TimeSpan BlockTime => Milestones.OnBlockAt - Milestones.OffBlockAt;
    public TimeSpan AirTime => Milestones.LandingAt - Milestones.TakeOffAt;
    public static CompletedFlight Create(CompletedFlightCreationArgs args) => new(args);
}
