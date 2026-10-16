using BookingSystem.Domain.Shared;
using BookingSystem.TestUtilities;
using BookingSystem.TestUtilities.Builders;

namespace BookingSystem.UnitTests.Domain;

/// <summary>
/// Short value helpers shared by the unit tests. Every test uses <see cref="UtcNow"/> as "now";
/// the system clock is never read. Rooms and bookings are created with the builders in
/// <c>BookingSystem.TestUtilities.Builders</c>.
/// </summary>
internal static class TestData
{
    public static readonly DateTime UtcNow = TestClock.UtcNow;

    public static Money Usd(decimal amount) => Money.Create(amount).Value;

    public static TimeSlot Slot(DateTime start, TimeSpan duration) =>
        new TimeSlotBuilder().Starting(start).Lasting(duration).Build();

    public static TimeSlot SlotStartingIn(TimeSpan startsIn, TimeSpan? duration = null) =>
        new TimeSlotBuilder().StartingIn(startsIn).Lasting(duration ?? TimeSlotBuilder.DefaultDuration).Build();
}
