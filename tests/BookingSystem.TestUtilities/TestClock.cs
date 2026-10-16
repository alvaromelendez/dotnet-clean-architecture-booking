namespace BookingSystem.TestUtilities;

/// <summary>
/// The fixed "now" of every test. Tests never read the system clock: builders, fake clocks and
/// expected values are all expressed relative to <see cref="UtcNow"/>, so results do not depend
/// on when or where the suite runs.
/// </summary>
public static class TestClock
{
    public static readonly DateTime UtcNow = new(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc);
}
