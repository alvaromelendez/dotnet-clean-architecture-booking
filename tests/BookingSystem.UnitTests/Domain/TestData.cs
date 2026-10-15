using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.UnitTests.Domain;

/// <summary>
/// Deterministic builders shared by the domain tests. Every test uses
/// <see cref="UtcNow"/> as "now"; the system clock is never read.
/// </summary>
internal static class TestData
{
    public static readonly DateTime UtcNow = new(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);

    public static Money Usd(decimal amount) => Money.Create(amount).Value;

    public static TimeSlot Slot(DateTime start, TimeSpan duration) =>
        TimeSlot.Create(start, start + duration).Value;

    public static TimeSlot SlotStartingIn(TimeSpan startsIn, TimeSpan? duration = null) =>
        Slot(UtcNow + startsIn, duration ?? TimeSpan.FromHours(2));

    public static Room ActiveRoom(int capacity = 10, decimal hourlyRate = 20m) =>
        Room.Create("Meeting Room A", capacity, Usd(hourlyRate)).Value;

    /// <summary>
    /// Creates a pending booking through <see cref="BookingService"/>, the only way to create one.
    /// The default slot starts three days after <see cref="UtcNow"/> and lasts two hours.
    /// </summary>
    public static Booking PendingBooking(Room? room = null, TimeSlot? slot = null)
    {
        room ??= ActiveRoom();
        slot ??= SlotStartingIn(TimeSpan.FromDays(3));

        return new BookingService().Reserve(room, UserId.New(), slot, 2, [], UtcNow).Value;
    }

    public static Booking ConfirmedBooking(Room? room = null, TimeSlot? slot = null)
    {
        var booking = PendingBooking(room, slot);
        booking.Confirm(UtcNow);

        return booking;
    }

    /// <summary>
    /// Creates a booking in <paramref name="status"/> by walking the state machine,
    /// with its domain events cleared.
    /// </summary>
    public static Booking BookingWithStatus(BookingStatus status, Room? room = null, TimeSlot? slot = null)
    {
        var booking = status == BookingStatus.Pending
            ? PendingBooking(room, slot)
            : ConfirmedBooking(room, slot);

        switch (status)
        {
            case BookingStatus.Cancelled:
                booking.Cancel(UtcNow);
                break;
            case BookingStatus.Completed:
                booking.Complete(booking.TimeSlot.End);
                break;
        }

        Assert.Equal(status, booking.Status);
        booking.ClearDomainEvents();

        return booking;
    }
}
