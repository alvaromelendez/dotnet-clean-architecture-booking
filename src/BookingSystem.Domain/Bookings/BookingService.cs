using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Domain.Bookings;

/// <summary>
/// Stateless domain service that creates bookings. It owns the rules that span the
/// <see cref="Room"/> and <see cref="Booking"/> aggregates, such as availability.
/// </summary>
public sealed class BookingService
{
    public const int MinAttendees = 1;
    public const int MaxDaysInAdvance = 100;

    /// <summary>
    /// Reserves <paramref name="room"/> for <paramref name="slot"/>, returning a
    /// <see cref="BookingStatus.Pending"/> booking priced by <see cref="Room.CalculatePrice"/>.
    /// </summary>
    /// <param name="existingBookingsForRoom">
    /// Bookings of <paramref name="room"/> that may conflict with <paramref name="slot"/>.
    /// Only pending and confirmed bookings block the slot.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="existingBookingsForRoom"/> contains a booking of another room,
    /// or <paramref name="utcNow"/> is not UTC.
    /// </exception>
    public Result<Booking> Reserve(
        Room room,
        UserId userId,
        TimeSlot slot,
        int attendees,
        IReadOnlyCollection<Booking> existingBookingsForRoom,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(existingBookingsForRoom);
        DateTimeGuard.EnsureUtc(utcNow);

        if (userId == default)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        if (existingBookingsForRoom.Any(booking => booking.RoomId != room.Id))
        {
            throw new ArgumentException(
                "All existing bookings must belong to the room being reserved.",
                nameof(existingBookingsForRoom));
        }

        if (!room.IsActive)
        {
            return BookingErrors.RoomInactive;
        }

        if (attendees < MinAttendees)
        {
            return BookingErrors.InvalidAttendees;
        }

        if (attendees > room.Capacity)
        {
            return BookingErrors.CapacityExceeded;
        }

        if (slot.Start <= utcNow)
        {
            return BookingErrors.StartNotInFuture;
        }

        if (slot.Start > utcNow.AddDays(MaxDaysInAdvance))
        {
            return BookingErrors.TooFarInAdvance;
        }

        if (existingBookingsForRoom.Any(booking => booking.IsActive && booking.TimeSlot.OverlapsWith(slot)))
        {
            return BookingErrors.Overlapping;
        }

        var totalPrice = room.CalculatePrice(slot);

        return Booking.Create(room.Id, userId, slot, attendees, totalPrice, utcNow);
    }
}
