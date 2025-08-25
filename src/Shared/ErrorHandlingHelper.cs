using System.Collections.ObjectModel;
using ErrorOr;
using Microsoft.AspNetCore.Http;

namespace Shared;

public static class ErrorHandlingHelper
{
    public static readonly ReadOnlyDictionary<int, string> ErrorMessages = new Dictionary<int, string>
    {
        { 400, "Validation Error" },
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
        if (error.Type == ErrorType.Validation)
        {
            return Results.Problem(
                statusCode: statusCode,
                title: ErrorMessages[statusCode],
                detail: error.Description);
        }
        if (error.Type == ErrorType.NotFound)
        {
            return Results.Problem(
                statusCode: statusCode,
                title: ErrorMessages[statusCode],
                detail: error.Description);
        }
        return Results.Problem(
            statusCode: statusCode, title: error.Description);
    }
}
