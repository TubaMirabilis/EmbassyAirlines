using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Airports.Api.Lambda.FunctionalTests.Extensions;

internal static class ProblemDetailsExtensions
{
    public static ProblemDetails WithValidationError(this ProblemDetails pd, string error)
    {
        pd.Detail = error;
        pd.Status = 400;
        pd.Title = ErrorHandlingHelper.ErrorMessages[(int)pd.Status];
        pd.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1";
        return pd;
    }
    public static ProblemDetails WithConflictError(this ProblemDetails pd, string error)
    {
        pd.Detail = error;
        pd.Status = 409;
        pd.Title = ErrorHandlingHelper.ErrorMessages[(int)pd.Status];
        pd.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10";
        return pd;
    }
    public static ProblemDetails WithQueryError(this ProblemDetails pd, string error)
    {
        pd.Detail = error;
        pd.Status = 404;
        pd.Title = ErrorHandlingHelper.ErrorMessages[(int)pd.Status];
        pd.Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
        return pd;
    }
}
