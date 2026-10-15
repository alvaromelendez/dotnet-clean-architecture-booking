using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Builders that go through the real domain factories. Names are unique per call because the
/// database is shared across tests and room names must be unique.
/// </summary>
internal static class TestData
{
    public static readonly DateTime UtcNow = new(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);

    private static readonly BookingService BookingService = new();

    public static Room NewRoom(int capacity = 10, decimal hourlyRate = 20m, string? name = null) =>
        Room.Create(name ?? $"Room {Guid.NewGuid():N}", capacity, Money.Create(hourlyRate).Value).Value;

    public static TimeSlot Slot(DateTime start, TimeSpan duration) =>
        TimeSlot.Create(start, start + duration).Value;

    public static TimeSlot SlotStartingIn(TimeSpan startsIn, TimeSpan? duration = null) =>
        Slot(UtcNow + startsIn, duration ?? TimeSpan.FromHours(2));

    /// <summary>
    /// Creates a pending booking without an availability check, simulating requests that raced.
    /// </summary>
    public static Booking NewBooking(Room room, TimeSlot slot, UserId? userId = null, DateTime? createdAtUtc = null) =>
        BookingService.Reserve(room, userId ?? UserId.New(), slot, 2, [], createdAtUtc ?? UtcNow).Value;
}
