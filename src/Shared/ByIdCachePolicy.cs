using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace Shared;

//
// Summary:
//     Provides a policy for caching responses by id.
public sealed class ByIdCachePolicy : IOutputCachePolicy
{
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context,
        CancellationToken cancellation)
    {
        var idRouteVal = context.HttpContext.Request.RouteValues["id"];
        if (idRouteVal is null)
        {
            return ValueTask.CompletedTask;
        }
        context.Tags.Add(idRouteVal.ToString()!);
        var attemptOutputCaching = AttemptOutputCaching(context);
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attemptOutputCaching;
        context.AllowCacheStorage = attemptOutputCaching;
        context.AllowLocking = true;
        context.CacheVaryByRules.QueryKeys = "*";
        return ValueTask.CompletedTask;
    }
    //
    // Summary:
    //     This method is called when the response is being served from the
    //     cache.
    //
    // Parameters:
    //   context:
    //     The Microsoft.AspNetCore.OutputCaching.OutputCacheContext.
    //
    //   cancellation:
    //     The System.Threading.CancellationToken.
    //
    // Returns:
    //     A System.Threading.Tasks.ValueTask.
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context,
        CancellationToken cancellation)
    {
        return ValueTask.CompletedTask;
    }
    //
    // Summary:
    //     This method is called when the response is being served from either
    //     the cache or from the server.
    //
    // Parameters:
    //   context:
    //     The Microsoft.AspNetCore.OutputCaching.OutputCacheContext.
    //
    //   cancellation:
    //     The System.Threading.CancellationToken.
    //
    // Returns:
    //     A System.Threading.Tasks.ValueTask.
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context,
        CancellationToken cancellation)
    {
        var response = context.HttpContext.Response;
        if (!StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }
        return ValueTask.CompletedTask;
    }
    //
    // Summary:
    //     Determines whether the response should be stored in the cache.
    //
    // Parameters:
    //   context:
    //     The Microsoft.AspNetCore.OutputCaching.OutputCacheContext.
    //
    // Returns:
    //     True if the response should be stored in the cache; otherwise, false.
    private bool AttemptOutputCaching(OutputCacheContext context)
    {
        var request = context.HttpContext.Request;
        if (!HttpMethods.IsGet(request.Method) &&
            !HttpMethods.IsHead(request.Method))
        {
            return false;
        }
        if (!StringValues.IsNullOrEmpty(request.Headers.Authorization) ||
            request.HttpContext.User?.Identity?.IsAuthenticated == true)
        {
            return false;
        }
        return true;
    }
}