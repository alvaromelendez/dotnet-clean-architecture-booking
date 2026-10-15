using System.Reflection;
using BookingSystem.Api.Endpoints;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookingSystem.Api.Infrastructure;

internal static class EndpointExtensions
{
    /// <summary>
    /// Registers every concrete <see cref="IEndpoint"/> found in <paramref name="assembly"/>.
    /// </summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpoints = assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type));

        services.TryAddEnumerable(endpoints);

        return services;
    }

    /// <summary>
    /// Maps every registered <see cref="IEndpoint"/> onto <paramref name="routeBuilder"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder routeBuilder, IServiceProvider services)
    {
        foreach (var endpoint in services.GetRequiredService<IEnumerable<IEndpoint>>())
        {
            endpoint.MapEndpoint(routeBuilder);
        }

        return routeBuilder;
    }
}
