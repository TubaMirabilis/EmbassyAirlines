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
    public static IResult HandleProblems(IEnumerable<Error> errors)
    {
        var errorsList = errors.ToList();
        if (errorsList.Count is 0)
        {
            return Results.Problem();
        }
        if (errorsList.TrueForAll(error => error.Type == ErrorType.Validation))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error",
                detail: string.Join("\n", errorsList.Select(error => error.Description)));
        }
        return HandleProblem(errorsList[0]);
    }
    private static IResult HandleProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
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
