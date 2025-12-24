using Shared;

namespace Aircraft.Core.Models;

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
    public int Kilograms { get; }
}
