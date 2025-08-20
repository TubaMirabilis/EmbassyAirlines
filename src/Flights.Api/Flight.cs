namespace Flights.Api;

public sealed class Flight
{
    private Flight(Guid id)
    {
        Id = id;
    }
    #pragma warning disable CS8618
    private Flight()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public static Flight Create(Guid id) => new(id);
}
