namespace Flights.Api.Domain;

public sealed class Aircraft
{
    // Private constructor
    private Aircraft(string typeDesignator, string registration)
    {
        Id = Guid.NewGuid();
        TypeDesignator = typeDesignator;
        Registration = registration;
    }
#pragma warning disable CS8618
    private Aircraft()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; private set; }
    public string TypeDesignator { get; private set; }
    public string Registration { get; private set; }
    public static Aircraft Create(string typeDesignator, string registration) => new(typeDesignator, registration);
}
