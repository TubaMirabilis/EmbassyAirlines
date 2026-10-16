using Flights.Infrastructure.Database;
using Microsoft.Extensions.DependencyInjection;

namespace Flights.Api.Lambda.FunctionalTests;

public sealed class ConcurrencyFunctionalTestWebAppFactory : FunctionalTestWebAppFactory
{
    protected override void ConfigureAdditionalTestServices(
        IServiceCollection services)
    {
        services.AddSingleton<ForceFlightConcurrencyConflictInterceptor>();
        services.ConfigureDbContext<ApplicationDbContext>((sp, options) => options.AddInterceptors(sp.GetRequiredService<ForceFlightConcurrencyConflictInterceptor>()));
    }
}
