using BookingSystem.Application.Behaviors;
using BookingSystem.Domain.Bookings;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, pipeline behaviors and
    /// domain services. Behaviors run in registration order: Logging → Validation → handler.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddSingleton<BookingService>();

        return services;
    }
}
