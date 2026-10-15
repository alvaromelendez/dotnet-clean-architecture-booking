using System.Net;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Rooms;
using BookingSystem.Domain.Shared;
using BookingSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;

namespace BookingSystem.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public sealed class UnexpectedErrorTests(PostgresFixture fixture)
{
    private const string SensitiveMessage = "Npgsql failure: Host=db.internal;Password=super-secret";

    [Fact]
    public async Task UnhandledException_Returns500ProblemWithoutInternalDetails()
    {
        await using var factory = fixture.Api.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRoomQueries>();
            services.AddScoped<IRoomQueries, ThrowingRoomQueries>();
        }));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/rooms");

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.InternalServerError, "Server.UnexpectedError");
        var body = problem.GetRawText();
        body.ShouldNotContain("super-secret");
        body.ShouldNotContain("Npgsql");
        body.ShouldNotContain(nameof(ThrowingRoomQueries));
        body.ShouldNotContain("   at ");
        problem.TryGetProperty("detail", out _).ShouldBeFalse();
    }

    private sealed class ThrowingRoomQueries : IRoomQueries
    {
        public Task<RoomDto?> GetByIdAsync(Guid roomId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(SensitiveMessage);

        public Task<IReadOnlyList<RoomDto>> ListAsync(bool? isActive, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(SensitiveMessage);

        public Task<IReadOnlyList<RoomDto>> GetAvailableAsync(
            TimeSlot slot, int minCapacity, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(SensitiveMessage);
    }
}
