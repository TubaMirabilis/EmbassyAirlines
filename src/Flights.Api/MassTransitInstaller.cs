using MassTransit;

namespace Flights.Api;

public static class MassTransitInstaller
{
    public static IServiceCollection AddMassTransit(this IServiceCollection services, IConfiguration config)
    {
        var region = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-2";
        services.AddMassTransit(x =>
{
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: config["MassTransit:Scope"]));
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(region, h => h.Scope(config["MassTransit:Scope"], scopeTopics: true));
        cfg.ConfigureEndpoints(context);
    });
});
        return services;
    }
}
