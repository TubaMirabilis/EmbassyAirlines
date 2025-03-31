using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Shared;

public static class Ensure
{
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
    public static void GreaterThanZero(
        int value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName);
        }
    }
}
