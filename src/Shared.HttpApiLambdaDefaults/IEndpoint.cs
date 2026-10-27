using Microsoft.AspNetCore.Routing;

namespace Shared.HttpApiLambdaDefaults;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
