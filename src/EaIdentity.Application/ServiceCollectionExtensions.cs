using EaCommon.PipelineBehaviors;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace EaIdentity.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services
            .AddSingleton(typeof(IPipelineBehavior<,>), typeof(ErrorLoggingBehavior<,>))
            .AddSingleton(typeof(IPipelineBehavior<,>), typeof(MessageValidatorBehavior<,>));
    }
}