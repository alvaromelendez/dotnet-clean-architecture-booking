using System.Net;
using BookingSystem.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingSystem.IntegrationTests.Api;

[Collection(PostgresCollection.Name)]
public sealed class ConcurrencyTests(PostgresFixture fixture)
{
    private readonly HttpClient _client = fixture.Api.CreateClient();

    [Fact]
    public async Task CreateBooking_TwoOverlappingRequestsInParallel_ExactlyOneSucceeds()
    {
        var roomId = await _client.CreateRoomAsync();
        var start = new DateTimeOffset(TestData.UtcNow.AddDays(4));

        // Whichever path detects the clash (the application check or the database exclusion
        // constraint, when both requests pass the check), the loser must get a 409.
        var responses = await Task.WhenAll(
            _client.PostBookingAsync(roomId, start, start.AddHours(2)),
            _client.PostBookingAsync(roomId, start.AddMinutes(30), start.AddHours(1)));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        var loser = responses.Single(response => response.StatusCode != HttpStatusCode.Created);
        var problem = await loser.ShouldBeProblemAsync(HttpStatusCode.Conflict);
        problem.GetProperty("code").GetString().ShouldBeOneOf("Booking.Overlapping", "Booking.SlotNoLongerAvailable");
    }
}
