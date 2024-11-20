using FluentValidation.Results;

namespace Flights.Api.Extensions;

internal static class ValidationResultExtensions
{
    public static bool IsValid(this ValidationResult validationResult, out string formattedErrors)
    {
        if (validationResult.Errors.Count == 0)
        {
            formattedErrors = string.Empty;
            return true;
        }
        var errors = validationResult.Errors
                                     .Select(e => e.ErrorMessage)
                                     .ToList();
        formattedErrors = string.Join("\n", errors);
        return false;
    }
}
