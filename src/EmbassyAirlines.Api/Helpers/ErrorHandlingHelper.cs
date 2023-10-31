using ErrorOr;

namespace EmbassyAirlines.Api.Helpers;

public static class ErrorHandlingHelper
{
    public static IResult HandleProblems(List<Error> errors)
    {
        if (errors.Count is 0)
        {
            return Results.Problem();
        }
        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            return Results.BadRequest(errors);
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
        return Results.Problem(statusCode: statusCode, title: error.Description);
    }
}
