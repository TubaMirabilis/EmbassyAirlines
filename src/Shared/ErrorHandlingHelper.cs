using System.Collections.ObjectModel;
using ErrorOr;
using Microsoft.AspNetCore.Http;

namespace Shared;

public static class ErrorHandlingHelper
{
    public static readonly ReadOnlyDictionary<int, string> ErrorMessages = new Dictionary<int, string>
    {
        { 400, "Validation Error" },
        { 409, "Conflict Error" },
        { 404, "Query Error" }
    }.AsReadOnly();
    public static IResult HandleProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError,
        };
        return statusCode switch
        {
            StatusCodes.Status500InternalServerError => Results.Problem(statusCode: statusCode, title: error.Description),
            _ => Results.Problem(statusCode: statusCode, title: ErrorMessages[statusCode], detail: error.Description)
        };
    }
}
