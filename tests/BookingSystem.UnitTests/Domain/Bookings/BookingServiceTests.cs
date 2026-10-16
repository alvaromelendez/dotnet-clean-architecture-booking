using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Bookings.Events;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Domain.Bookings;

public sealed class BookingServiceTests
{
    private readonly BookingService _bookingService = new();

    [Fact]
    public void Reserve_WithValidRequest_ReturnsPendingBookingWithCalculatedPrice()
    {
        var room = new RoomBuilder().WithCapacity(10).WithHourlyRate(20m).Build();
        var userId = UserId.New();
        var slot = SlotStartingIn(TimeSpan.FromDays(1), TimeSpan.FromMinutes(90));

        var result = _bookingService.Reserve(room, userId, slot, 4, [], UtcNow);

        Assert.True(result.IsSuccess);
        var booking = result.Value;
        Assert.Equal(room.Id, booking.RoomId);
        Assert.Equal(userId, booking.UserId);
        Assert.Equal(slot, booking.TimeSlot);
        Assert.Equal(4, booking.Attendees);
        Assert.Equal(Usd(30m), booking.TotalPrice);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(UtcNow, booking.CreatedAtUtc);
        Assert.Null(booking.ConfirmedAtUtc);
        Assert.Null(booking.CancelledAtUtc);
        Assert.Null(booking.RefundAmount);
    }

    [Fact]
    public void Reserve_WithValidRequest_RaisesBookingCreatedEvent()
    {
        var room = new RoomBuilder().Build();
        var userId = UserId.New();
        var slot = SlotStartingIn(TimeSpan.FromDays(1));

        var booking = _bookingService.Reserve(room, userId, slot, 2, [], UtcNow).Value;

        var domainEvent = Assert.IsType<BookingCreatedDomainEvent>(Assert.Single(booking.DomainEvents));
        Assert.Equal(booking.Id, domainEvent.BookingId);
        Assert.Equal(room.Id, domainEvent.RoomId);
        Assert.Equal(userId, domainEvent.UserId);
        Assert.Equal(slot, domainEvent.TimeSlot);
        Assert.Equal(booking.TotalPrice, domainEvent.TotalPrice);
    }

    // Rule a: the room must be active.
    [Fact]
    public void Reserve_WhenRoomIsInactive_ReturnsRoomInactiveError()
    {
        var result = Reserve(new RoomBuilder().Inactive().Build());

        Assert.Equal(BookingErrors.RoomInactive, result.Error);
    }

    // Rule b: attendees between 1 and the room capacity.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_WithLessThanOneAttendee_ReturnsInvalidAttendeesError(int attendees)
    {
        var result = Reserve(new RoomBuilder().Build(), attendees: attendees);

        Assert.Equal(BookingErrors.InvalidAttendees, result.Error);
    }

    [Fact]
    public void Reserve_WithMinimumAttendees_ReturnsBooking()
    {
        var result = Reserve(new RoomBuilder().Build(), attendees: BookingService.MinAttendees);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingService.MinAttendees, result.Value.Attendees);
    }

    [Fact]
    public void Reserve_WithAttendeesAboveCapacity_ReturnsCapacityExceededError()
    {
        var result = Reserve(new RoomBuilder().WithCapacity(6).Build(), attendees: 7);

        Assert.Equal(BookingErrors.CapacityExceeded, result.Error);
    }

    [Fact]
    public void Reserve_WithAttendeesEqualToCapacity_ReturnsBooking()
    {
        var result = Reserve(new RoomBuilder().WithCapacity(6).Build(), attendees: 6);

        Assert.True(result.IsSuccess);
    }

    // Rule c: the slot must start in the future.
    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void Reserve_WhenSlotDoesNotStartInTheFuture_ReturnsStartNotInFutureError(int startOffsetMinutes)
    {
        var slot = Slot(UtcNow.AddMinutes(startOffsetMinutes), TimeSpan.FromHours(1));

        var result = Reserve(new RoomBuilder().Build(), slot);

        Assert.Equal(BookingErrors.StartNotInFuture, result.Error);
    }

    // Rule d: no more than 100 days in advance.
    [Fact]
    public void Reserve_WhenSlotStartsMoreThanMaximumDaysAhead_ReturnsTooFarInAdvanceError()
    {
        var slot = SlotStartingIn(TimeSpan.FromDays(BookingService.MaxDaysInAdvance) + TimeSpan.FromMinutes(1));

        var result = Reserve(new RoomBuilder().Build(), slot);

        Assert.Equal(BookingErrors.TooFarInAdvance, result.Error);
    }

    [Fact]
    public void Reserve_WhenSlotStartsExactlyMaximumDaysAhead_ReturnsBooking()
    {
        var slot = SlotStartingIn(TimeSpan.FromDays(BookingService.MaxDaysInAdvance));

        var result = Reserve(new RoomBuilder().Build(), slot);

        Assert.True(result.IsSuccess);
    }

    // Rule e: no overlap with active (pending or confirmed) bookings.
    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    public void Reserve_WhenOverlappingActiveBooking_ReturnsOverlappingError(BookingStatus existingStatus)
    {
        var room = new RoomBuilder().Build();
        var existing = new BookingBuilder().ForRoom(room).StartingIn(TimeSpan.FromDays(3)).InStatus(existingStatus).Build();
        var overlappingSlot = Slot(existing.TimeSlot.Start.AddMinutes(30), TimeSpan.FromHours(2));

        var result = Reserve(room, overlappingSlot, existingBookings: [existing]);

        Assert.Equal(BookingErrors.Overlapping, result.Error);
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public void Reserve_WhenOverlappingInactiveBooking_ReturnsBooking(BookingStatus existingStatus)
    {
        var room = new RoomBuilder().Build();
        var existing = new BookingBuilder().ForRoom(room).StartingIn(TimeSpan.FromDays(3)).InStatus(existingStatus).Build();

        var result = Reserve(room, existing.TimeSlot, existingBookings: [existing]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Reserve_WhenAdjacentToActiveBooking_ReturnsBooking()
    {
        var room = new RoomBuilder().Build();
        var existing = new BookingBuilder().ForRoom(room).StartingIn(TimeSpan.FromDays(3)).Confirmed().Build();
        var adjacentSlot = Slot(existing.TimeSlot.End, TimeSpan.FromHours(1));

        var result = Reserve(room, adjacentSlot, existingBookings: [existing]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Reserve_WithExistingBookingOfAnotherRoom_ThrowsArgumentException()
    {
        var otherRoomBooking = new BookingBuilder().ForRoom(new RoomBuilder().Build()).Build();

        Assert.Throws<ArgumentException>(
            () => Reserve(new RoomBuilder().Build(), existingBookings: [otherRoomBooking]));
    }

    [Fact]
    public void Reserve_WithNonUtcNow_ThrowsArgumentException()
    {
        var localNow = DateTime.SpecifyKind(UtcNow, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () => _bookingService.Reserve(new RoomBuilder().Build(), UserId.New(), SlotStartingIn(TimeSpan.FromDays(1)), 1, [], localNow));
    }

    /// <summary>
    /// The clock is validated before any business rule, so a non-UTC "now" is reported as the
    /// programming error it is, not hidden behind an expected failure such as an inactive room.
    /// </summary>
    [Fact]
    public void Reserve_WithNonUtcNowAndInactiveRoom_ThrowsInsteadOfReturningRuleError()
    {
        var localNow = DateTime.SpecifyKind(UtcNow, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () => _bookingService.Reserve(new RoomBuilder().Inactive().Build(), UserId.New(), SlotStartingIn(TimeSpan.FromDays(1)), 1, [], localNow));
    }

    [Fact]
    public void Reserve_WithDefaultUserId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => _bookingService.Reserve(new RoomBuilder().Build(), default, SlotStartingIn(TimeSpan.FromDays(1)), 1, [], UtcNow));

        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void Reserve_WithNullArguments_ThrowsArgumentNullException()
    {
        var room = new RoomBuilder().Build();
        var slot = SlotStartingIn(TimeSpan.FromDays(1));

        Assert.Equal(
            "room",
            Assert.Throws<ArgumentNullException>(() => _bookingService.Reserve(null!, UserId.New(), slot, 1, [], UtcNow)).ParamName);
        Assert.Equal(
            "slot",
            Assert.Throws<ArgumentNullException>(() => _bookingService.Reserve(room, UserId.New(), null!, 1, [], UtcNow)).ParamName);
        Assert.Equal(
            "existingBookingsForRoom",
            Assert.Throws<ArgumentNullException>(() => _bookingService.Reserve(room, UserId.New(), slot, 1, null!, UtcNow)).ParamName);
    }

    private Result<Booking> Reserve(
        Room room,
        TimeSlot? slot = null,
        int attendees = 2,
        IReadOnlyCollection<Booking>? existingBookings = null) =>
        _bookingService.Reserve(
            room,
            UserId.New(),
            slot ?? SlotStartingIn(TimeSpan.FromDays(1)),
            attendees,
            existingBookings ?? [],
            UtcNow);
}
