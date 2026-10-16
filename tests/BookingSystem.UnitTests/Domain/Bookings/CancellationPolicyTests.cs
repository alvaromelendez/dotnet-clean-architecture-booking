using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Shared;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Domain.Bookings;

public sealed class CancellationPolicyTests
{
    private static readonly Money TotalPrice = Usd(100m);

    [Theory]
    [MemberData(nameof(RefundCases))]
    public void CalculateRefund_ConfirmedBooking_ReturnsRefundBasedOnNotice(
        TimeSpan notice, decimal expectedRefund)
    {
        var slot = SlotStartingIn(notice);

        var result = CancellationPolicy.CalculateRefund(BookingStatus.Confirmed, TotalPrice, slot, UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(Usd(expectedRefund), result.Value);
    }

    public static TheoryData<TimeSpan, decimal> RefundCases => new()
    {
        // Full refund: 24 hours or more.
        { TimeSpan.FromDays(3), 100m },
        { TimeSpan.FromHours(24), 100m },
        // Partial refund: at least 2 hours and less than 24 hours.
        { TimeSpan.FromHours(24) - TimeSpan.FromSeconds(1), 50m },
        { TimeSpan.FromHours(10), 50m },
        { TimeSpan.FromHours(2), 50m },
        // No refund: less than 2 hours.
        { TimeSpan.FromHours(2) - TimeSpan.FromSeconds(1), 0m },
        { TimeSpan.FromMinutes(1), 0m },
    };

    [Theory]
    [InlineData(48)]
    [InlineData(1)]
    public void CalculateRefund_PendingBooking_ReturnsZeroBecauseNothingWasCharged(int hoursBeforeStart)
    {
        var slot = SlotStartingIn(TimeSpan.FromHours(hoursBeforeStart));

        var result = CancellationPolicy.CalculateRefund(BookingStatus.Pending, TotalPrice, slot, UtcNow);

        Assert.Equal(Money.Zero(), result.Value);
    }

    [Fact]
    public void CalculateRefund_InOtherCurrency_ReturnsRefundInSameCurrency()
    {
        var totalPrice = Money.Create(80m, "EUR").Value;
        var slot = SlotStartingIn(TimeSpan.FromDays(2));

        var result = CancellationPolicy.CalculateRefund(BookingStatus.Confirmed, totalPrice, slot, UtcNow);

        Assert.Equal(totalPrice, result.Value);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    public void CalculateRefund_WhenSlotStartsNow_ReturnsAlreadyStartedError(BookingStatus status)
    {
        var slot = Slot(UtcNow, TimeSpan.FromHours(1));

        var result = CancellationPolicy.CalculateRefund(status, TotalPrice, slot, UtcNow);

        Assert.Equal(BookingErrors.AlreadyStarted, result.Error);
    }

    [Fact]
    public void CalculateRefund_WhenSlotAlreadyStarted_ReturnsAlreadyStartedError()
    {
        var slot = Slot(UtcNow.AddMinutes(-30), TimeSpan.FromHours(1));

        var result = CancellationPolicy.CalculateRefund(BookingStatus.Confirmed, TotalPrice, slot, UtcNow);

        Assert.Equal(BookingErrors.AlreadyStarted, result.Error);
    }

    [Fact]
    public void CalculateRefund_WithNullArguments_ThrowsArgumentNullException()
    {
        var slot = SlotStartingIn(TimeSpan.FromDays(1));

        Assert.Throws<ArgumentNullException>(
            () => CancellationPolicy.CalculateRefund(BookingStatus.Pending, null!, slot, UtcNow));
        Assert.Throws<ArgumentNullException>(
            () => CancellationPolicy.CalculateRefund(BookingStatus.Pending, TotalPrice, null!, UtcNow));
    }

    [Fact]
    public void CalculateRefund_WithNonUtcNow_ThrowsArgumentException()
    {
        var slot = SlotStartingIn(TimeSpan.FromDays(1));

        Assert.Throws<ArgumentException>(() => CancellationPolicy.CalculateRefund(
            BookingStatus.Confirmed, TotalPrice, slot, DateTime.SpecifyKind(UtcNow, DateTimeKind.Local)));
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public void CalculateRefund_ForTerminalStatus_ThrowsArgumentOutOfRangeException(BookingStatus status)
    {
        var slot = SlotStartingIn(TimeSpan.FromDays(1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CancellationPolicy.CalculateRefund(status, TotalPrice, slot, UtcNow));
    }
}
