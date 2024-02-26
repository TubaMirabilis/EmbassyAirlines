using ErrorOr;
using Microsoft.AspNetCore.Http;

namespace Shared;

public static class ErrorHandlingHelper
{
    public static IResult HandleProblems(List<Error> errors)
    {
        if (errors.Count is 0)
        {
            return Results.Problem();
        }
        if (errors.TrueForAll(error => error.Type == ErrorType.Validation))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error",
                extensions: new Dictionary<string, object?>
                {
                    ["errors"] = errors
                        .Select(error => error.Description).ToList()
                });
        }
        return HandleProblem(errors[0]);
    }
    private static IResult HandleProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError,
        };
        return Results.Problem(
            statusCode: statusCode, title: error.Description);
    }
}