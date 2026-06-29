using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Shared.Extensions;

public static class HttpStatusCodeExtensions
{
    public static ProblemDetails CreateExpectedProblemDetails(this HttpStatusCode statusCode, string detail) => statusCode switch
    {
        HttpStatusCode.BadRequest => new ProblemDetails().WithValidationError(detail),
        HttpStatusCode.NotFound => new ProblemDetails().WithQueryError(detail),
        HttpStatusCode.Conflict => new ProblemDetails().WithConflictError(detail),
        _ => throw new InvalidOperationException($"Unsupported status code: {statusCode}")
    };
}
