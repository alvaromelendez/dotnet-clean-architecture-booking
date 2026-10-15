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
        var room = ActiveRoom(capacity: 10, hourlyRate: 20m);
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
        var room = ActiveRoom();
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
        var room = ActiveRoom();
        room.Deactivate();

        var result = Reserve(room);

        Assert.Equal(BookingErrors.RoomInactive, result.Error);
    }

    // Rule b: attendees between 1 and the room capacity.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_WithLessThanOneAttendee_ReturnsInvalidAttendeesError(int attendees)
    {
        var result = Reserve(ActiveRoom(), attendees: attendees);

        Assert.Equal(BookingErrors.InvalidAttendees, result.Error);
    }

    [Fact]
    public void Reserve_WithAttendeesAboveCapacity_ReturnsCapacityExceededError()
    {
        var result = Reserve(ActiveRoom(capacity: 6), attendees: 7);

        Assert.Equal(BookingErrors.CapacityExceeded, result.Error);
    }

    [Fact]
    public void Reserve_WithAttendeesEqualToCapacity_ReturnsBooking()
    {
        var result = Reserve(ActiveRoom(capacity: 6), attendees: 6);

        Assert.True(result.IsSuccess);
    }

    // Rule c: the slot must start in the future.
    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void Reserve_WhenSlotDoesNotStartInTheFuture_ReturnsStartNotInFutureError(int startOffsetMinutes)
    {
        var slot = Slot(UtcNow.AddMinutes(startOffsetMinutes), TimeSpan.FromHours(1));

        var result = Reserve(ActiveRoom(), slot);

        Assert.Equal(BookingErrors.StartNotInFuture, result.Error);
    }

    // Rule d: no more than 100 days in advance.
    [Fact]
    public void Reserve_WhenSlotStartsMoreThanMaximumDaysAhead_ReturnsTooFarInAdvanceError()
    {
        var slot = SlotStartingIn(TimeSpan.FromDays(BookingService.MaxDaysInAdvance) + TimeSpan.FromMinutes(1));

        var result = Reserve(ActiveRoom(), slot);

        Assert.Equal(BookingErrors.TooFarInAdvance, result.Error);
    }

    [Fact]
    public void Reserve_WhenSlotStartsExactlyMaximumDaysAhead_ReturnsBooking()
    {
        var slot = SlotStartingIn(TimeSpan.FromDays(BookingService.MaxDaysInAdvance));

        var result = Reserve(ActiveRoom(), slot);

        Assert.True(result.IsSuccess);
    }

    // Rule e: no overlap with active (pending or confirmed) bookings.
    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    public void Reserve_WhenOverlappingActiveBooking_ReturnsOverlappingError(BookingStatus existingStatus)
    {
        var room = ActiveRoom();
        var existing = BookingWithStatus(existingStatus, room, SlotStartingIn(TimeSpan.FromDays(3)));
        var overlappingSlot = Slot(existing.TimeSlot.Start.AddMinutes(30), TimeSpan.FromHours(2));

        var result = Reserve(room, overlappingSlot, existingBookings: [existing]);

        Assert.Equal(BookingErrors.Overlapping, result.Error);
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public void Reserve_WhenOverlappingInactiveBooking_ReturnsBooking(BookingStatus existingStatus)
    {
        var room = ActiveRoom();
        var existing = BookingWithStatus(existingStatus, room, SlotStartingIn(TimeSpan.FromDays(3)));

        var result = Reserve(room, existing.TimeSlot, existingBookings: [existing]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Reserve_WhenAdjacentToActiveBooking_ReturnsBooking()
    {
        var room = ActiveRoom();
        var existing = ConfirmedBooking(room, SlotStartingIn(TimeSpan.FromDays(3)));
        var adjacentSlot = Slot(existing.TimeSlot.End, TimeSpan.FromHours(1));

        var result = Reserve(room, adjacentSlot, existingBookings: [existing]);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Reserve_WithExistingBookingOfAnotherRoom_ThrowsArgumentException()
    {
        var otherRoomBooking = PendingBooking(ActiveRoom());

        Assert.Throws<ArgumentException>(
            () => Reserve(ActiveRoom(), existingBookings: [otherRoomBooking]));
    }

    [Fact]
    public void Reserve_WithNonUtcNow_ThrowsArgumentException()
    {
        var localNow = DateTime.SpecifyKind(UtcNow, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () => _bookingService.Reserve(ActiveRoom(), UserId.New(), SlotStartingIn(TimeSpan.FromDays(1)), 1, [], localNow));
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
