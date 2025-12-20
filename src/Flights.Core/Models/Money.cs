using Shared;

namespace Flights.Core.Models;

public sealed record Money
{
    public Money(decimal amount)
    {
        Ensure.ZeroOrGreater(amount);
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }
    private Money()
    {
    }
    public decimal Amount { get; }
}
