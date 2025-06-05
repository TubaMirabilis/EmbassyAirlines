using Shared;

namespace Aircraft.Api.Lambda;

public sealed record Weight
{
    public Weight(int kilograms)
    {
        Ensure.GreaterThanZero(kilograms);
        Kilograms = kilograms;
    }
    private Weight()
    {
    }
    public int Kilograms { get; init; }
}
