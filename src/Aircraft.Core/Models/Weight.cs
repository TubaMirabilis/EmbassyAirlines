using Shared;

namespace Aircraft.Core.Models;

public sealed record Weight
{
    public Weight(int kilograms)
    {
        Ensure.ZeroOrGreater(kilograms);
        Kilograms = kilograms;
    }
    private Weight()
    {
    }
    public int Kilograms { get; }
}
