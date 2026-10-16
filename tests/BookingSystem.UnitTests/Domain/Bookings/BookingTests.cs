using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Bookings.Events;
using BookingSystem.Domain.Shared;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Domain.Bookings;

public sealed class BookingTests
{
    // Confirm

    [Fact]
    public void Confirm_WhenPending_ConfirmsAndRaisesBookingConfirmedEvent()
    {
        var booking = new BookingBuilder().Build();
        var confirmedAt = UtcNow.AddMinutes(5);

        var result = booking.Confirm(confirmedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(confirmedAt, booking.ConfirmedAtUtc);
        var domainEvent = Assert.IsType<BookingConfirmedDomainEvent>(Assert.Single(booking.DomainEvents));
        Assert.Equal(booking.Id, domainEvent.BookingId);
        Assert.Equal(confirmedAt, domainEvent.ConfirmedAtUtc);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public void Confirm_WhenNotPending_ReturnsNotPendingErrorWithoutChanges(BookingStatus status)
    {
        var booking = new BookingBuilder().InStatus(status).Build();

        var result = booking.Confirm(UtcNow);

        Assert.Equal(BookingErrors.NotPending, result.Error);
        Assert.Equal(status, booking.Status);
        Assert.Empty(booking.DomainEvents);
    }

    // Cancel

    [Fact]
    public void Cancel_WhenPending_CancelsWithZeroRefundAndRaisesBookingCancelledEvent()
    {
        var booking = new BookingBuilder().Build();

        var result = booking.Cancel(UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(UtcNow, booking.CancelledAtUtc);
        Assert.Equal(Money.Zero(), booking.RefundAmount);
        var domainEvent = Assert.IsType<BookingCancelledDomainEvent>(Assert.Single(booking.DomainEvents));
        Assert.Equal(booking.Id, domainEvent.BookingId);
        Assert.Equal(booking.RoomId, domainEvent.RoomId);
        Assert.Equal(Money.Zero(), domainEvent.RefundAmount);
        Assert.Equal(UtcNow, domainEvent.CancelledAtUtc);
    }

    [Fact]
    public void Cancel_WhenConfirmed_CancelsWithPolicyRefundAndRaisesBookingCancelledEvent()
    {
        var booking = new BookingBuilder().StartingIn(TimeSpan.FromHours(10)).Confirmed().Build();
        var expectedRefund = booking.TotalPrice.Multiply(CancellationPolicy.PartialRefundRate);

        var result = booking.Cancel(UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(UtcNow, booking.CancelledAtUtc);
        Assert.Equal(expectedRefund, booking.RefundAmount);
        var domainEvent = Assert.IsType<BookingCancelledDomainEvent>(Assert.Single(booking.DomainEvents));
        Assert.Equal(expectedRefund, domainEvent.RefundAmount);
    }

    [Fact]
    public void Cancel_WhenConfirmedWithFullNotice_RefundsTotalPrice()
    {
        var booking = new BookingBuilder().StartingIn(TimeSpan.FromDays(3)).Confirmed().Build();

        booking.Cancel(UtcNow);

        Assert.Equal(booking.TotalPrice, booking.RefundAmount);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    public void Cancel_AfterSlotStarted_ReturnsAlreadyStartedErrorWithoutChanges(BookingStatus status)
    {
        var booking = new BookingBuilder().InStatus(status).Build();

        var result = booking.Cancel(booking.TimeSlot.Start);

        Assert.Equal(BookingErrors.AlreadyStarted, result.Error);
        Assert.Equal(status, booking.Status);
        Assert.Null(booking.CancelledAtUtc);
        Assert.Null(booking.RefundAmount);
        Assert.Empty(booking.DomainEvents);
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public void Cancel_WhenTerminal_ReturnsNotCancellableErrorWithoutChanges(BookingStatus status)
    {
        var booking = new BookingBuilder().InStatus(status).Build();

        var result = booking.Cancel(UtcNow);

        Assert.Equal(BookingErrors.NotCancellable, result.Error);
        Assert.Equal(status, booking.Status);
        Assert.Empty(booking.DomainEvents);
    }

    // Complete

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    public void Complete_WhenConfirmedAndSlotEnded_CompletesAndRaisesBookingCompletedEvent(int minutesAfterEnd)
    {
        var booking = new BookingBuilder().Confirmed().Build();
        var completedAt = booking.TimeSlot.End.AddMinutes(minutesAfterEnd);

        var result = booking.Complete(completedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        var domainEvent = Assert.IsType<BookingCompletedDomainEvent>(Assert.Single(booking.DomainEvents));
        Assert.Equal(booking.Id, domainEvent.BookingId);
        Assert.Equal(completedAt, domainEvent.CompletedAtUtc);
    }

    [Fact]
    public void Complete_WhenConfirmedButSlotNotEnded_ReturnsNotYetEndedError()
    {
        var booking = new BookingBuilder().Confirmed().Build();

        var result = booking.Complete(booking.TimeSlot.End.AddTicks(-1));

        Assert.Equal(BookingErrors.NotYetEnded, result.Error);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Empty(booking.DomainEvents);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public void Complete_WhenNotConfirmed_ReturnsNotConfirmedErrorWithoutChanges(BookingStatus status)
    {
        var booking = new BookingBuilder().InStatus(status).Build();

        var result = booking.Complete(booking.TimeSlot.End);

        Assert.Equal(BookingErrors.NotConfirmed, result.Error);
        Assert.Equal(status, booking.Status);
        Assert.Empty(booking.DomainEvents);
    }

    // Clock guard

    /// <summary>
    /// The clock is validated before the state machine: a non-UTC "now" is a programming error
    /// even when the transition itself would be rejected or would not need the time.
    /// </summary>
    [Fact]
    public void Cancel_WithNonUtcNowOnTerminalBooking_ThrowsArgumentException()
    {
        var booking = new BookingBuilder().Completed().Build();

        Assert.Throws<ArgumentException>(() => booking.Cancel(DateTime.SpecifyKind(booking.TimeSlot.End, DateTimeKind.Local)));
    }

    [Fact]
    public void Complete_WithNonUtcNowAfterSlotEnded_ThrowsArgumentException()
    {
        var booking = new BookingBuilder().Confirmed().Build();

        Assert.Throws<ArgumentException>(() => booking.Complete(DateTime.SpecifyKind(booking.TimeSlot.End, DateTimeKind.Local)));
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void StateTransitions_WithNonUtcNow_ThrowArgumentException()
    {
        var booking = new BookingBuilder().Build();
        var localNow = DateTime.SpecifyKind(UtcNow, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => booking.Confirm(localNow));
        Assert.Throws<ArgumentException>(() => booking.Cancel(localNow));
        Assert.Throws<ArgumentException>(() => booking.Complete(localNow));
    }
}
