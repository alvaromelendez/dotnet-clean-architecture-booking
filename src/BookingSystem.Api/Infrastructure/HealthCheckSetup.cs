using BookingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace BookingSystem.Api.Infrastructure;

internal static class HealthCheckSetup
{
    private const string ReadyTag = "ready";

    /// <summary>
    /// Readiness checks PostgreSQL through the application's own <see cref="ApplicationDbContext"/>
    /// (a real round trip with the same connection settings the API uses).
    /// </summary>
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("postgresql", tags: [ReadyTag]);

        return services;
    }

    /// <summary>
    /// <c>/health/live</c> runs no checks: it only proves the process answers requests.
    /// <c>/health/ready</c> also checks dependencies. Both are anonymous, unversioned and exempt
    /// from rate limiting so orchestrator probes are never throttled.
    /// </summary>
    public static WebApplication MapApiHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .DisableRateLimiting()
            .ExcludeFromDescription();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) })
            .DisableRateLimiting()
            .ExcludeFromDescription();

        return app;
    }
}
