using Shared;

namespace Aircraft.Core.Models;

public sealed class CompletedFlight
{
    private CompletedFlight(Guid id, string departureAirportIcao, string arrivalAirportIcao, FlightMilestones milestones, DateTimeOffset createdAt)
    {
        Ensure.NotEmpty(id);
        Ensure.NotNullOrEmpty(departureAirportIcao);
        Ensure.NotNullOrEmpty(arrivalAirportIcao);
        Id = id;
        CreatedAt = createdAt;
        DepartureAirportIcao = departureAirportIcao;
        ArrivalAirportIcao = arrivalAirportIcao;
        Milestones = milestones;
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
    public static CompletedFlight Create(Guid id, string departureAirportIcao, string arrivalAirportIcao, FlightMilestones milestones, DateTimeOffset createdAt) => new(id, departureAirportIcao, arrivalAirportIcao, milestones, createdAt);
}
