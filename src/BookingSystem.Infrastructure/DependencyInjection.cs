using BookingSystem.Application.Abstractions.Clock;
using BookingSystem.Application.Abstractions.Notifications;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Infrastructure.Clock;
using BookingSystem.Infrastructure.Notifications;
using BookingSystem.Infrastructure.Persistence;
using BookingSystem.Infrastructure.Persistence.Queries;
using BookingSystem.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingSystem.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Name of the connection string read from configuration (<c>ConnectionStrings:Database</c>).
    /// </summary>
    public const string ConnectionStringName = "Database";

    /// <summary>
    /// Registers persistence (PostgreSQL through EF Core), the unit of work and infrastructure services.
    /// </summary>
    /// <exception cref="InvalidOperationException">The connection string is not configured.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<ApplicationDbContext>(
            options => ApplicationDbContextOptions.Configure(options, connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IRoomQueries, RoomQueries>();
        services.AddScoped<IBookingQueries, BookingQueries>();

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<INotificationSender, LoggingNotificationSender>();

        return services;
    }
}
