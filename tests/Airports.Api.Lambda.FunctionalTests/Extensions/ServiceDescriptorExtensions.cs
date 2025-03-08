using Microsoft.Extensions.DependencyInjection;

namespace Airports.Api.Lambda.FunctionalTests.Extensions;

public static class ServiceDescriptorExtensions
{
    public static bool IsMassTransitService(this ServiceDescriptor descriptor)
    {
        var namespaceName = descriptor.ServiceType
                                      .Namespace;
        return !string.IsNullOrWhiteSpace(namespaceName) &&
               namespaceName.Contains("MassTransit", StringComparison.OrdinalIgnoreCase);
    }
}
