using Microsoft.AspNetCore.Mvc;

namespace Flights.Api.FunctionalTests.Extensions;

internal static class ProblemDetailsExtensions
{
    public static ProblemDetails WithValidationError(this ProblemDetails pd, string error)
    {
        pd.Detail = error;
        pd.Status = 400;
        pd.Title = "Validation Error";
        pd.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
        return pd;
    }
}
