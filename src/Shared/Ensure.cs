using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Shared;

public static class Ensure
{
    // Method to guard against Guid.Empty:
    public static void NotEmpty(
        Guid value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be an empty GUID.", paramName);
        }
    }
    public static void NotNullOrEmpty(
            [NotNull] string? value,
            [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty or whitespace.", paramName);
        }
    }
    public static void ZeroOrGreater(
        decimal value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.");
        }
    }
    public static void GreaterThanZero(
        int value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }
    public static void LessThanOrEqualTo(
        int value,
        int maxValue,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value > maxValue)
        {
            throw new ArgumentOutOfRangeException(paramName, $"Value must be less than or equal to {maxValue}.");
        }
    }
}
