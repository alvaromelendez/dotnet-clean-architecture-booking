using System.Net;
using System.Net.Http.Json;
using System.Text;
using BookingSystem.Api.Contracts.Common;
using BookingSystem.Api.Contracts.Rooms;
using BookingSystem.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingSystem.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public sealed class ErrorMappingTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Api.CreateClient();

    [Fact]
    public async Task CreateRoom_WithInvalidBody_Returns400WithErrorsPerField()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/rooms", new CreateRoomRequest("", 5, 10m, "US"), ApiTestClient.Json);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        var errors = problem.GetProperty("errors");
        errors.GetProperty("name").GetArrayLength().ShouldBeGreaterThan(0);
        errors.GetProperty("currency").GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CreateRoom_WithDomainRuleViolation_Returns400WithDomainCode()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/rooms", new CreateRoomRequest($"Room {Guid.NewGuid():N}", 0, 10m, "USD"), ApiTestClient.Json);

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Room.InvalidCapacity");
    }

    [Fact]
    public async Task CreateRoom_WithMalformedJson_Returns400Problem()
    {
        using var content = new StringContent("{ \"name\": ", Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/rooms", content);

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Request.Invalid");
    }

    [Fact]
    public async Task ListRooms_WithMalformedFilter_Returns400Problem()
    {
        var response = await _client.GetAsync("/api/v1/rooms?isActive=not-a-boolean");

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Request.Invalid");
    }

    [Fact]
    public async Task GetRoom_WhenMissing_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/rooms/{Guid.NewGuid()}");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Room.NotFound");
    }

    [Fact]
    public async Task ConfirmBooking_WhenMissing_Returns404()
    {
        var response = await _client.PostAsync($"/api/v1/bookings/{Guid.NewGuid()}/confirm", content: null);

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Booking.NotFound");
    }

    [Fact]
    public async Task CreateBooking_ForMissingRoom_Returns404()
    {
        var start = new DateTimeOffset(TestData.UtcNow.AddDays(2));

        var response = await _client.PostBookingAsync(Guid.NewGuid(), start, start.AddHours(1));

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Room.NotFound");
    }

    [Fact]
    public async Task CreateBooking_OverlappingExistingBooking_Returns409()
    {
        var roomId = await _client.CreateRoomAsync();
        var start = new DateTimeOffset(TestData.UtcNow.AddDays(2));
        (await _client.PostBookingAsync(roomId, start, start.AddHours(2))).StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await _client.PostBookingAsync(roomId, start.AddHours(1), start.AddHours(3));

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Booking.Overlapping");
    }

    [Fact]
    public async Task ConfirmBooking_Twice_Returns409ForInvalidTransition()
    {
        var roomId = await _client.CreateRoomAsync();
        var start = new DateTimeOffset(TestData.UtcNow.AddDays(2));
        var bookingId = (await (await _client.PostBookingAsync(roomId, start, start.AddHours(1)))
            .ReadAsync<ResourceCreatedResponse>()).Id;
        (await _client.PostAsync($"/api/v1/bookings/{bookingId}/confirm", null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await _client.PostAsync($"/api/v1/bookings/{bookingId}/confirm", content: null);

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Booking.NotPending");
    }

    [Fact]
    public async Task GetUserBookings_WithPageSizeOutOfRange_Returns400WithPageSizeError()
    {
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}/bookings?pageSize=1000");

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Validation.Failed");
        problem.GetProperty("errors").GetProperty("pageSize").GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task UnknownRoute_Returns404Problem()
    {
        var response = await _client.GetAsync("/api/v1/does-not-exist");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Http.NotFound");
    }

    [Fact]
    public async Task UnsupportedMethod_Returns405Problem()
    {
        var response = await _client.DeleteAsync($"/api/v1/rooms/{Guid.NewGuid()}");

        await response.ShouldBeProblemAsync(HttpStatusCode.MethodNotAllowed, "Http.MethodNotAllowed");
    }

    [Fact]
    public async Task UnsupportedApiVersion_ReturnsProblem()
    {
        var response = await _client.GetAsync("/api/v2/rooms");

        response.Content.Headers.ContentType?.MediaType.ShouldBe(ApiTestClient.ProblemJson);
        ((int)response.StatusCode).ShouldBeInRange(400, 499);
    }
}
