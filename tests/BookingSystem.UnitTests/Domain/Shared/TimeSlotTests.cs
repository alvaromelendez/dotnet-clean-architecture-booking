using BookingSystem.Domain.Shared;

namespace BookingSystem.UnitTests.Domain.Shared;

public sealed class TimeSlotTests
{
    private static readonly DateTime Start = new(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidUtcRange_ReturnsTimeSlot()
    {
        var end = Start.AddHours(2);

        var result = TimeSlot.Create(Start, end);

        Assert.True(result.IsSuccess);
        Assert.Equal(Start, result.Value.Start);
        Assert.Equal(end, result.Value.End);
    }

    [Theory]
    [InlineData(DateTimeKind.Local, DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified, DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Utc, DateTimeKind.Local)]
    [InlineData(DateTimeKind.Utc, DateTimeKind.Unspecified)]
    public void Create_WithNonUtcBoundary_ReturnsNotUtcError(DateTimeKind startKind, DateTimeKind endKind)
    {
        var start = DateTime.SpecifyKind(Start, startKind);
        var end = DateTime.SpecifyKind(Start.AddHours(1), endKind);

        var result = TimeSlot.Create(start, end);

        Assert.Equal(TimeSlotErrors.NotUtc, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void Create_WhenEndIsNotAfterStart_ReturnsEndNotAfterStartError(int endOffsetMinutes)
    {
        var result = TimeSlot.Create(Start, Start.AddMinutes(endOffsetMinutes));

        Assert.Equal(TimeSlotErrors.EndNotAfterStart, result.Error);
    }

    [Fact]
    public void Create_WithDurationBelowMinimum_ReturnsTooShortError()
    {
        var result = TimeSlot.Create(Start, Start.AddMinutes(29));

        Assert.Equal(TimeSlotErrors.TooShort, result.Error);
    }

    [Fact]
    public void Create_WithExactlyMinimumDuration_ReturnsTimeSlot()
    {
        var result = TimeSlot.Create(Start, Start + TimeSlot.MinimumDuration);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithExactlyMaximumDuration_ReturnsTimeSlot()
    {
        var result = TimeSlot.Create(Start, Start + TimeSlot.MaximumDuration);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithDurationAboveMaximum_ReturnsTooLongError()
    {
        var result = TimeSlot.Create(Start, Start.AddHours(8).AddMinutes(1));

        Assert.Equal(TimeSlotErrors.TooLong, result.Error);
    }

    [Fact]
    public void Duration_ForValidSlot_ReturnsDifferenceBetweenEndAndStart()
    {
        var slot = TimeSlot.Create(Start, Start.AddMinutes(90)).Value;

        Assert.Equal(TimeSpan.FromMinutes(90), slot.Duration);
    }

    /// <summary>
    /// The reference slot is 10:00–12:00. Offsets are relative to 10:00.
    /// </summary>
    [Theory]
    [InlineData(120, 60, false)] // adjacent after: 12:00–13:00
    [InlineData(-60, 60, false)] // adjacent before: 09:00–10:00
    [InlineData(180, 60, false)] // disjoint: 13:00–14:00
    [InlineData(30, 60, true)]   // contained: 10:30–11:30
    [InlineData(-60, 240, true)] // containing: 09:00–13:00
    [InlineData(-30, 60, true)]  // partial at start: 09:30–10:30
    [InlineData(90, 60, true)]   // partial at end: 11:30–12:30
    [InlineData(0, 120, true)]   // identical: 10:00–12:00
    public void OverlapsWith_OtherSlot_ReturnsExpectedResultInBothDirections(
        int otherStartOffsetMinutes, int otherDurationMinutes, bool expected)
    {
        var reference = TimeSlot.Create(Start, Start.AddHours(2)).Value;
        var otherStart = Start.AddMinutes(otherStartOffsetMinutes);
        var other = TimeSlot.Create(otherStart, otherStart.AddMinutes(otherDurationMinutes)).Value;

        Assert.Equal(expected, reference.OverlapsWith(other));
        Assert.Equal(expected, other.OverlapsWith(reference));
    }
}
