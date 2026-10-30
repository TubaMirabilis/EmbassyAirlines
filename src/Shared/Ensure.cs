using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Shared;

public static class Ensure
{
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
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value must be greater than or equal to zero.");
        }
    }
    public static void NotNull<T>(
        [NotNull] T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
    public static void GreaterThanZero(
        int value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.");
        }
    }
    public static void IsNonDecreasing(
        DateTimeOffset[] value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        for (var i = 1; i < value.Length; i++)
        {
            if (value[i] < value[i - 1])
            {
                throw new ArgumentException(
                    "Value must be a non-decreasing sequence.", paramName);
            }
        }
    }
}
