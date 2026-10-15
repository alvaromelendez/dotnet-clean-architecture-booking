using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Shared;

/// <summary>
/// A half-open UTC time interval <c>[Start, End)</c> that can be booked.
/// </summary>
public sealed record TimeSlot
{
    public static readonly TimeSpan MinimumDuration = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan MaximumDuration = TimeSpan.FromHours(8);

    private TimeSlot(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Inclusive UTC start.
    /// </summary>
    public DateTime Start { get; }

    /// <summary>
    /// Exclusive UTC end.
    /// </summary>
    public DateTime End { get; }

    public TimeSpan Duration => End - Start;

    public static Result<TimeSlot> Create(DateTime start, DateTime end)
    {
        if (start.Kind != DateTimeKind.Utc || end.Kind != DateTimeKind.Utc)
        {
            return TimeSlotErrors.NotUtc;
        }

        if (end <= start)
        {
            return TimeSlotErrors.EndNotAfterStart;
        }

        var duration = end - start;

        if (duration < MinimumDuration)
        {
            return TimeSlotErrors.TooShort;
        }

        if (duration > MaximumDuration)
        {
            return TimeSlotErrors.TooLong;
        }

        return new TimeSlot(start, end);
    }

    /// <summary>
    /// Whether both slots share at least one instant. Because slots are half-open,
    /// a slot ending at 10:00 does not overlap one starting at 10:00.
    /// </summary>
    public bool OverlapsWith(TimeSlot other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Start < other.End && other.Start < End;
    }
}
