using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Infrastructure.Persistence.Queries;
using BookingSystem.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingSystem.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class RoomQueriesTests(PostgresFixture fixture)
{
    [Fact]
    public async Task GetByIdAsync_WhenRoomExists_ProjectsDto()
    {
        var room = TestData.NewRoom(capacity: 4, hourlyRate: 12.75m);
        await SaveAsync([room]);

        await using var dbContext = fixture.CreateDbContext();
        var dto = await new RoomQueries(dbContext).GetByIdAsync(room.Id.Value);

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(room.Id.Value);
        dto.Name.ShouldBe(room.Name);
        dto.Capacity.ShouldBe(4);
        dto.HourlyRate.ShouldBe(12.75m);
        dto.Currency.ShouldBe("USD");
        dto.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoomDoesNotExist_ReturnsNull()
    {
        await using var dbContext = fixture.CreateDbContext();

        (await new RoomQueries(dbContext).GetByIdAsync(Guid.NewGuid())).ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_WithActiveFilter_ReturnsOnlyRoomsInThatState()
    {
        var active = TestData.NewRoom();
        var inactive = TestData.NewRoom();
        inactive.Deactivate();
        await SaveAsync([active, inactive]);

        await using var dbContext = fixture.CreateDbContext();
        var queries = new RoomQueries(dbContext);
        var activeIds = (await queries.ListAsync(isActive: true)).Select(room => room.Id).ToList();
        var inactiveIds = (await queries.ListAsync(isActive: false)).Select(room => room.Id).ToList();
        var allIds = (await queries.ListAsync(isActive: null)).Select(room => room.Id).ToList();

        activeIds.ShouldContain(active.Id.Value);
        activeIds.ShouldNotContain(inactive.Id.Value);
        inactiveIds.ShouldContain(inactive.Id.Value);
        inactiveIds.ShouldNotContain(active.Id.Value);
        allIds.ShouldContain(active.Id.Value);
        allIds.ShouldContain(inactive.Id.Value);
    }

    [Fact]
    public async Task GetAvailableAsync_ReturnsActiveRoomsWithCapacityAndNoConflictingBooking()
    {
        var slot = TestData.SlotStartingIn(TimeSpan.FromDays(7));
        var free = TestData.NewRoom(capacity: 8);
        var busy = TestData.NewRoom(capacity: 8);
        var inactive = TestData.NewRoom(capacity: 8);
        var tooSmall = TestData.NewRoom(capacity: 2);
        var freedByCancellation = TestData.NewRoom(capacity: 8);
        var adjacentOnly = TestData.NewRoom(capacity: 8);
        inactive.Deactivate();
        await SaveAsync([free, busy, inactive, tooSmall, freedByCancellation, adjacentOnly]);

        var busyBooking = TestData.NewBooking(busy, TestData.Slot(slot.Start.AddMinutes(30), TimeSpan.FromHours(1)));
        busyBooking.Confirm(TestData.UtcNow);
        var cancelledBooking = TestData.NewBooking(freedByCancellation, slot);
        cancelledBooking.Cancel(TestData.UtcNow);
        var adjacentBooking = TestData.NewBooking(adjacentOnly, TestData.Slot(slot.End, TimeSpan.FromHours(1)));
        await SaveAsync(bookings: [busyBooking, cancelledBooking, adjacentBooking]);

        await using var dbContext = fixture.CreateDbContext();
        var availableIds = (await new RoomQueries(dbContext).GetAvailableAsync(slot, minCapacity: 4))
            .Select(room => room.Id)
            .ToHashSet();

        availableIds.ShouldContain(free.Id.Value);
        availableIds.ShouldContain(freedByCancellation.Id.Value);
        availableIds.ShouldContain(adjacentOnly.Id.Value);
        availableIds.ShouldNotContain(busy.Id.Value);
        availableIds.ShouldNotContain(inactive.Id.Value);
        availableIds.ShouldNotContain(tooSmall.Id.Value);
    }

    private async Task SaveAsync(Room[]? rooms = null, Booking[]? bookings = null)
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Rooms.AddRange(rooms ?? []);
        dbContext.Bookings.AddRange(bookings ?? []);
        await PostgresFixture.CreateUnitOfWork(dbContext).SaveChangesAsync();
    }
}
