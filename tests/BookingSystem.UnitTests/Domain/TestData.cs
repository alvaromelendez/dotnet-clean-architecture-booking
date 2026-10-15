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
}
