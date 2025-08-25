using Shared;

namespace Flights.Api;

internal sealed record Money
{
    public Money(decimal amount)
    {
        Ensure.GreaterThanZero(amount);
        Amount = amount;
    }
    private Money()
    {
    }
    public decimal Amount { get; init; }
}
