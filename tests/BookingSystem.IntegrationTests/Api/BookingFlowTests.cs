using System.Net;
using System.Net.Http.Json;
using BookingSystem.Api.Contracts.Bookings;
using BookingSystem.Api.Contracts.Common;
using BookingSystem.Api.Contracts.Rooms;
using BookingSystem.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingSystem.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public sealed class BookingFlowTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Api.CreateClient();

    [Fact]
    public async Task HappyPath_CreateRoomBookConfirmCancel_ReturnsExpectedStatusesAndRefund()
    {
        // Create room -> 201 + Location to GET /rooms/{id}.
        var createRoom = await _client.PostAsJsonAsync(
            "/api/v1/rooms",
            new CreateRoomRequest($"Court {Guid.NewGuid():N}", 6, 30m, "USD"),
            ApiTestClient.Json);
        createRoom.StatusCode.ShouldBe(HttpStatusCode.Created);
        var roomId = (await createRoom.ReadAsync<ResourceCreatedResponse>()).Id;
        createRoom.Headers.Location!.ToString().ShouldEndWith($"/api/v1/rooms/{roomId}");
        (await _client.GetAsync(createRoom.Headers.Location)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Book 3 days ahead of the frozen clock (>= 24 h notice => full refund once confirmed).
        var start = new DateTimeOffset(TestData.UtcNow.AddDays(3).AddHours(1), TimeSpan.Zero);
        var createBooking = await _client.PostBookingAsync(roomId, start, start.AddHours(2), attendees: 4);
        createBooking.StatusCode.ShouldBe(HttpStatusCode.Created);
        var bookingId = (await createBooking.ReadAsync<ResourceCreatedResponse>()).Id;
        createBooking.Headers.Location!.ToString().ShouldEndWith($"/api/v1/bookings/{bookingId}");

        var booking = await (await _client.GetAsync(createBooking.Headers.Location)).ReadAsync<BookingResponse>();
        booking.Status.ShouldBe(BookingStatusContract.Pending);
        booking.TotalPrice.ShouldBe(60m);
        booking.RefundAmount.ShouldBeNull();

        // Confirm -> 204.
        var confirm = await _client.PostAsync($"/api/v1/bookings/{bookingId}/confirm", content: null);
        confirm.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Cancel -> 200 with the refund.
        var cancel = await _client.PostAsync($"/api/v1/bookings/{bookingId}/cancel", content: null);
        cancel.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cancellation = await cancel.ReadAsync<BookingCancellationResponse>();
        cancellation.ShouldBe(new BookingCancellationResponse(bookingId, 60m, "USD"));

        var cancelled = await (await _client.GetAsync($"/api/v1/bookings/{bookingId}")).ReadAsync<BookingResponse>();
        cancelled.Status.ShouldBe(BookingStatusContract.Cancelled);
        cancelled.RefundAmount.ShouldBe(60m);
    }

    [Fact]
    public async Task CreateBooking_WithOffsetDates_StoresTheSameInstantInUtc()
    {
        var roomId = await _client.CreateRoomAsync();

        // 12:00 at +02:00 is 10:00 UTC.
        var start = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.FromHours(2));
        var response = await _client.PostBookingAsync(roomId, start, start.AddHours(1));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var booking = await (await _client.GetAsync(response.Headers.Location)).ReadAsync<BookingResponse>();
        booking.StartUtc.ShouldBe(new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc));
        booking.StartUtc.Kind.ShouldBe(DateTimeKind.Utc);
        booking.EndUtc.ShouldBe(new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetAvailableRooms_WithOffsetQuery_ExcludesRoomBookedForTheSameInstant()
    {
        var bookedRoom = await _client.CreateRoomAsync();
        var freeRoom = await _client.CreateRoomAsync();
        var start = new DateTimeOffset(2026, 3, 11, 10, 0, 0, TimeSpan.Zero);
        (await _client.PostBookingAsync(bookedRoom, start, start.AddHours(2))).StatusCode.ShouldBe(HttpStatusCode.Created);

        // Same window expressed at +02:00; '+' must be URL-encoded.
        var query = $"start={Uri.EscapeDataString("2026-03-11T12:00:00+02:00")}&end={Uri.EscapeDataString("2026-03-11T14:00:00+02:00")}";
        var rooms = await (await _client.GetAsync($"/api/v1/rooms/available?{query}"))
            .ReadAsync<List<RoomResponse>>();

        rooms.Select(room => room.Id).ShouldContain(freeRoom);
        rooms.Select(room => room.Id).ShouldNotContain(bookedRoom);
    }

    [Fact]
    public async Task GetUserBookings_WithStatusFilter_ReturnsPagedBookings()
    {
        var roomId = await _client.CreateRoomAsync();
        var userId = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 3, 12, 8, 0, 0, TimeSpan.Zero);
        for (var hour = 0; hour < 3; hour++)
        {
            (await _client.PostBookingAsync(roomId, start.AddHours(hour), start.AddHours(hour + 1), userId: userId))
                .StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        var page = await (await _client.GetAsync($"/api/v1/users/{userId}/bookings?status=Pending&page=1&pageSize=2"))
            .ReadAsync<PagedResponse<BookingResponse>>();

        page.TotalCount.ShouldBe(3);
        page.Items.Count.ShouldBe(2);
        page.HasNextPage.ShouldBeTrue();
        page.Items.ShouldAllBe(booking => booking.UserId == userId && booking.Status == BookingStatusContract.Pending);
    }
}
