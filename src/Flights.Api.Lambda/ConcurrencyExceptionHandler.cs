using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Flights.Api.Lambda;

internal sealed class ConcurrencyExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ConcurrencyExceptionHandler> _logger;
    public ConcurrencyExceptionHandler(ILogger<ConcurrencyExceptionHandler> logger) => _logger = logger;
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
        {
            return false;
        }

        _logger.LogWarning(
            exception,
            "A database concurrency conflict occurred while processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict Error",
            Detail = "The resource has changed or is no longer available. Refresh its state before retrying the operation.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10"
        };

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}
