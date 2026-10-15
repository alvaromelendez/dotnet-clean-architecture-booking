using System.Net;
using BookingSystem.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingSystem.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public sealed class HealthCheckTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Api.CreateClient();

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_WithDatabaseAvailable_Returns200Healthy(string path)
    {
        var response = await _client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task OpenApiDocument_InDevelopment_IsServed()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("/api/v1/bookings");
    }
}
