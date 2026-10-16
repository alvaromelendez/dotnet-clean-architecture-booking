using BookingSystem.Domain.Shared;

namespace BookingSystem.TestUtilities.Builders;

/// <summary>
/// Builds valid <see cref="TimeSlot"/>s. By default the slot starts <see cref="DefaultLeadTime"/>
/// after <see cref="TestClock.UtcNow"/> and lasts <see cref="DefaultDuration"/>, which satisfies every
/// reservation rule (future start, within the advance-booking window, 30 min to 8 h).
/// </summary>
public sealed class TimeSlotBuilder
{
    public static readonly TimeSpan DefaultLeadTime = TimeSpan.FromDays(3);
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(2);

    private DateTime _start = TestClock.UtcNow + DefaultLeadTime;
    private TimeSpan _duration = DefaultDuration;

    public TimeSlotBuilder Starting(DateTime startUtc)
    {
        _start = startUtc;
        return this;
    }

    /// <summary>
    /// Starts <paramref name="leadTime"/> after <see cref="TestClock.UtcNow"/>.
    /// </summary>
    public TimeSlotBuilder StartingIn(TimeSpan leadTime) => Starting(TestClock.UtcNow + leadTime);

    public TimeSlotBuilder Lasting(TimeSpan duration)
    {
        _duration = duration;
        return this;
    }

    public TimeSlot Build() => TimeSlot.Create(_start, _start + _duration).ValueOrThrow("create the time slot");
}
