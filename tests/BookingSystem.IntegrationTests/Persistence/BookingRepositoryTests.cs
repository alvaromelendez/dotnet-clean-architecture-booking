using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Infrastructure.Persistence.Repositories;
using BookingSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace BookingSystem.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class BookingRepositoryTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ThenSave_RoundTripsOwnedValueObjectsAndStatus()
    {
        var room = await SaveRoomAsync();
        var booking = TestData.NewBooking(room, TestData.SlotStartingIn(TimeSpan.FromDays(2), TimeSpan.FromMinutes(90)));
        await SaveNewAsync(booking);

        await using var dbContext = fixture.CreateDbContext();
        var loaded = await new BookingRepository(dbContext).GetByIdAsync(booking.Id);

        loaded.ShouldNotBeNull();
        loaded.RoomId.ShouldBe(room.Id);
        loaded.UserId.ShouldBe(booking.UserId);
        loaded.TimeSlot.ShouldBe(booking.TimeSlot);
        loaded.TimeSlot.Start.Kind.ShouldBe(DateTimeKind.Utc);
        loaded.TotalPrice.ShouldBe(booking.TotalPrice);
        loaded.Status.ShouldBe(BookingStatus.Pending);
        loaded.CreatedAtUtc.ShouldBe(TestData.UtcNow);
        loaded.RefundAmount.ShouldBeNull();
    }

    [Fact]
    public async Task Status_WhenPersisted_IsStoredAsText()
    {
        var booking = TestData.NewBooking(await SaveRoomAsync(), TestData.SlotStartingIn(TimeSpan.FromDays(2)));
        await SaveNewAsync(booking);

        await using var dbContext = fixture.CreateDbContext();
        var bookingId = booking.Id.Value;
        var storedStatus = await dbContext.Database
            .SqlQuery<string>($"SELECT status AS \"Value\" FROM bookings WHERE id = {bookingId}")
            .SingleAsync();

        storedStatus.ShouldBe("Pending");
    }

    [Fact]
    public async Task Cancel_ThenSave_PersistsRefundAmount()
    {
        var booking = TestData.NewBooking(await SaveRoomAsync(), TestData.SlotStartingIn(TimeSpan.FromDays(3)));
        await SaveNewAsync(booking);

        await using (var dbContext = fixture.CreateDbContext())
        {
            var tracked = await new BookingRepository(dbContext).GetByIdAsync(booking.Id);
            tracked!.Confirm(TestData.UtcNow).IsSuccess.ShouldBeTrue();
            tracked.Cancel(TestData.UtcNow).IsSuccess.ShouldBeTrue();
            await PostgresFixture.CreateUnitOfWork(dbContext).SaveChangesAsync();
        }

        await using var readContext = fixture.CreateDbContext();
        var loaded = await new BookingRepository(readContext).GetByIdAsync(booking.Id);

        loaded!.Status.ShouldBe(BookingStatus.Cancelled);
        loaded.RefundAmount.ShouldBe(booking.TotalPrice);
        loaded.CancelledAtUtc.ShouldBe(TestData.UtcNow);
    }

    [Fact]
    public async Task GetActiveByRoomAsync_ReturnsOnlyActiveBookingsOfTheRoomThatOverlapTheWindow()
    {
        var room = await SaveRoomAsync();
        var otherRoom = await SaveRoomAsync();
        var day = TestData.UtcNow.Date.AddDays(5);

        // Window: 10:00–12:00.
        var window = TestData.Slot(day.AddHours(10), TimeSpan.FromHours(2));
        var pendingOverlap = TestData.NewBooking(room, TestData.Slot(day.AddHours(9), TimeSpan.FromHours(2)));
        var confirmedOverlap = TestData.NewBooking(room, TestData.Slot(day.AddHours(11), TimeSpan.FromHours(2)));
        var adjacentBefore = TestData.NewBooking(room, TestData.Slot(day.AddHours(8), TimeSpan.FromHours(1)));
        var adjacentAfter = TestData.NewBooking(room, TestData.Slot(day.AddHours(13), TimeSpan.FromHours(1)));
        var otherRoomOverlap = TestData.NewBooking(otherRoom, window);
        confirmedOverlap.Confirm(TestData.UtcNow);
        await SaveNewAsync(pendingOverlap, confirmedOverlap, adjacentBefore, adjacentAfter, otherRoomOverlap);

        // A cancelled booking may share the slot of an active one (the exclusion constraint ignores it).
        var cancelledOverlap = TestData.NewBooking(room, TestData.Slot(day.AddHours(10), TimeSpan.FromMinutes(30)));
        cancelledOverlap.Cancel(TestData.UtcNow);
        await SaveNewAsync(cancelledOverlap);

        await using var dbContext = fixture.CreateDbContext();
        var active = await new BookingRepository(dbContext).GetActiveByRoomAsync(room.Id, window);

        active.Select(booking => booking.Id).ShouldBe([pendingOverlap.Id, confirmedOverlap.Id], ignoreOrder: true);
    }

    private async Task<Room> SaveRoomAsync()
    {
        var room = TestData.NewRoom();
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync();

        return room;
    }

    private async Task SaveNewAsync(params Booking[] bookings)
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new BookingRepository(dbContext);
        foreach (var booking in bookings)
        {
            await repository.AddAsync(booking);
        }

        await PostgresFixture.CreateUnitOfWork(dbContext).SaveChangesAsync();
    }
}
