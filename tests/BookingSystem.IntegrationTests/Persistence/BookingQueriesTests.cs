using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;
using BookingSystem.Infrastructure.Persistence.Queries;
using BookingSystem.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingSystem.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class BookingQueriesTests(PostgresFixture fixture)
{
    [Fact]
    public async Task GetByIdAsync_ForCancelledBooking_ProjectsAllFieldsIncludingRefund()
    {
        var room = TestData.NewRoom(hourlyRate: 30m);
        var booking = TestData.NewBooking(room, TestData.SlotStartingIn(TimeSpan.FromDays(3)));
        booking.Confirm(TestData.UtcNow.AddMinutes(5));
        booking.Cancel(TestData.UtcNow.AddMinutes(10));
        await SaveAsync(room, booking);

        await using var dbContext = fixture.CreateDbContext();
        var dto = await new BookingQueries(dbContext).GetByIdAsync(booking.Id.Value);

        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(booking.Id.Value);
        dto.RoomId.ShouldBe(room.Id.Value);
        dto.UserId.ShouldBe(booking.UserId.Value);
        dto.StartUtc.ShouldBe(booking.TimeSlot.Start);
        dto.EndUtc.ShouldBe(booking.TimeSlot.End);
        dto.Attendees.ShouldBe(booking.Attendees);
        dto.TotalPrice.ShouldBe(60m);
        dto.Currency.ShouldBe("USD");
        dto.Status.ShouldBe(BookingStatus.Cancelled);
        dto.CreatedAtUtc.ShouldBe(TestData.UtcNow);
        dto.ConfirmedAtUtc.ShouldBe(TestData.UtcNow.AddMinutes(5));
        dto.CancelledAtUtc.ShouldBe(TestData.UtcNow.AddMinutes(10));
        dto.RefundAmount.ShouldBe(60m);
    }

    [Fact]
    public async Task GetByIdAsync_ForPendingBooking_ProjectsNullRefund()
    {
        var room = TestData.NewRoom();
        var booking = TestData.NewBooking(room, TestData.SlotStartingIn(TimeSpan.FromDays(3)));
        await SaveAsync(room, booking);

        await using var dbContext = fixture.CreateDbContext();
        var dto = await new BookingQueries(dbContext).GetByIdAsync(booking.Id.Value);

        dto!.RefundAmount.ShouldBeNull();
        dto.ConfirmedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenBookingDoesNotExist_ReturnsNull()
    {
        await using var dbContext = fixture.CreateDbContext();

        (await new BookingQueries(dbContext).GetByIdAsync(Guid.NewGuid())).ShouldBeNull();
    }

    [Fact]
    public async Task GetByUserAsync_WithPaging_ReturnsNewestFirstWithTotalCount()
    {
        var (room, userId, bookings) = await SaveUserBookingsAsync(count: 5);

        await using var dbContext = fixture.CreateDbContext();
        var page = await new BookingQueries(dbContext).GetByUserAsync(userId.Value, status: null, page: 2, pageSize: 2);

        // Created at +0..+4 minutes; newest first => page 2 holds the bookings created at +2 and +1.
        page.TotalCount.ShouldBe(5);
        page.TotalPages.ShouldBe(3);
        page.Page.ShouldBe(2);
        page.Items.Select(dto => dto.Id).ShouldBe([bookings[2].Id.Value, bookings[1].Id.Value]);
        page.Items.ShouldAllBe(dto => dto.RoomId == room.Id.Value);
    }

    [Fact]
    public async Task GetByUserAsync_WithStatusFilter_ReturnsOnlyMatchingBookingsAndTheirCount()
    {
        var (_, userId, bookings) = await SaveUserBookingsAsync(count: 3, confirmFirst: true);

        await using var dbContext = fixture.CreateDbContext();
        var page = await new BookingQueries(dbContext).GetByUserAsync(
            userId.Value, BookingStatus.Confirmed, page: 1, pageSize: 10);

        page.TotalCount.ShouldBe(1);
        page.Items.ShouldHaveSingleItem().Id.ShouldBe(bookings[0].Id.Value);
    }

    [Fact]
    public async Task GetByUserAsync_WithEqualCreationDates_PagesDeterministicallyWithoutGapsOrDuplicates()
    {
        var (_, userId, bookings) = await SaveUserBookingsAsync(count: 4, sameCreationDate: true);

        await using var dbContext = fixture.CreateDbContext();
        var queries = new BookingQueries(dbContext);
        var seen = new List<Guid>();
        for (var pageNumber = 1; pageNumber <= 4; pageNumber++)
        {
            var page = await queries.GetByUserAsync(userId.Value, status: null, pageNumber, pageSize: 1);
            seen.Add(page.Items.ShouldHaveSingleItem().Id);
        }

        seen.ShouldBe(bookings.Select(booking => booking.Id.Value), ignoreOrder: true);
        seen.ShouldBeUnique();
    }

    [Fact]
    public async Task GetByUserAsync_ForUserWithoutBookings_ReturnsEmptyPage()
    {
        await using var dbContext = fixture.CreateDbContext();

        var page = await new BookingQueries(dbContext).GetByUserAsync(Guid.NewGuid(), status: null, page: 1, pageSize: 10);

        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(0);
    }

    private async Task<(Room Room, UserId UserId, List<Booking> Bookings)> SaveUserBookingsAsync(
        int count,
        bool confirmFirst = false,
        bool sameCreationDate = false)
    {
        var room = TestData.NewRoom();
        var userId = UserId.New();
        var day = TestData.UtcNow.Date.AddDays(10);

        // Non-overlapping slots (the exclusion constraint would reject overlaps) created a minute apart.
        var bookings = Enumerable.Range(0, count)
            .Select(index => TestData.NewBooking(
                room,
                TestData.Slot(day.AddHours(8 + index), TimeSpan.FromHours(1)),
                userId,
                TestData.UtcNow.AddMinutes(sameCreationDate ? 0 : index)))
            .ToList();

        if (confirmFirst)
        {
            bookings[0].Confirm(TestData.UtcNow);
        }

        await SaveAsync(room, [.. bookings]);

        return (room, userId, bookings);
    }

    private async Task SaveAsync(Room room, params Booking[] bookings)
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Rooms.Add(room);
        dbContext.Bookings.AddRange(bookings);
        await PostgresFixture.CreateUnitOfWork(dbContext).SaveChangesAsync();
    }
}
