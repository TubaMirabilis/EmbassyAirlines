using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shared.HttpApiLambdaDefaults.Extensions;
using Shared.ServiceDefaults;

namespace Shared.HttpApiLambdaDefaults;

public static class HttpApiLambdaExtensions
{
    public static TBuilder AddHttpApiLambdaDefaults<TBuilder>(
        this TBuilder builder,
        Assembly assembly)
        where TBuilder : IHostApplicationBuilder
    {
        builder.AddServiceDefaults();
        builder.Services.AddEndpoints(assembly);
        builder.Services.AddAWSLambdaHosting(
            LambdaEventSource.HttpApi);
        builder.Services
            .AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        return builder;
    }
    public static WebApplication UseDefaultPipeline(
        this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseMiddleware<RequestContextLoggingMiddleware>();
        app.UseSerilogRequestLogging();
        return app;
    }
    public static WebApplication MapDefaultEndpoints(
        this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        app.MapEndpoints();
        return app;
    }
}
