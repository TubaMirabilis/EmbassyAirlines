using Shared;

namespace Aircraft.Api.Lambda;

internal sealed record Weight
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
