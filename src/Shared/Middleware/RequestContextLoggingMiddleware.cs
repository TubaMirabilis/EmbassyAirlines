using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Shared.Middleware;

public class RequestContextLoggingMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    public RequestContextLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        using (LogContext.PushProperty("CorrelationId", GetCorrelationId(context)))
        {
            return _next.Invoke(context);
        }
    }
    private static string GetCorrelationId(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName];
        return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
    }
}
