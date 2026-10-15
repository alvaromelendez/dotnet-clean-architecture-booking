using BookingSystem.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BookingSystem.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public sealed class TestHostConfigurationTests(PostgresFixture fixture)
{
    /// <summary>
    /// The HTTP suite relies on test settings winning over appsettings.Development.json and user-secrets
    /// (otherwise it could migrate on startup, hit rate limits or talk to a developer's database).
    /// </summary>
    [Fact]
    public void Factory_WithTestSettings_OverridesDevelopmentConfiguration()
    {
        var configuration = fixture.Api.Services.GetRequiredService<IConfiguration>();

        configuration["Database:MigrateOnStartup"].ShouldBe("false");
        configuration["RateLimiting:PermitLimit"].ShouldBe("100000");
        configuration.GetConnectionString("Database").ShouldBe(fixture.ConnectionString);
    }
}
