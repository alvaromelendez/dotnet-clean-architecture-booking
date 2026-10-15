using BookingSystem.Application.Abstractions.Clock;
using BookingSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookingSystem.IntegrationTests.Api;

/// <summary>
/// Hosts the real API in memory against the Testcontainers database, with a frozen clock.
/// It runs as Development on purpose: error responses must not leak internals even there.
/// </summary>
public sealed class BookingApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // UseSetting values reach Program before AddInfrastructure reads the connection string.
        builder.UseSetting("ConnectionStrings:Database", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("RateLimiting:PermitLimit", "100000");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDateTimeProvider>();
            services.AddSingleton<IDateTimeProvider>(new FixedDateTimeProvider(TestData.UtcNow));
        });
    }
}
