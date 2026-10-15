using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Domain.Bookings;

/// <summary>
/// Determines how much of a booking's price is refunded when it is cancelled,
/// based on how far in advance the cancellation happens.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item>Notice of <see cref="FullRefundMinimumNotice"/> or more: <see cref="FullRefundRate"/>.</item>
///   <item>Notice of <see cref="PartialRefundMinimumNotice"/> or more: <see cref="PartialRefundRate"/>.</item>
///   <item>Shorter notice: <see cref="NoRefundRate"/>.</item>
///   <item>Pending bookings were never charged, so their refund is always zero.</item>
/// </list>
/// </remarks>
public static class CancellationPolicy
{
    public const decimal FullRefundRate = 1.0m;
    public const decimal PartialRefundRate = 0.5m;
    public const decimal NoRefundRate = 0m;

    public static readonly TimeSpan FullRefundMinimumNotice = TimeSpan.FromHours(24);
    public static readonly TimeSpan PartialRefundMinimumNotice = TimeSpan.FromHours(2);

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="status"/> is not cancellable.</exception>
    public static Result<Money> CalculateRefund(
        BookingStatus status,
        Money totalPrice,
        TimeSlot timeSlot,
        DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(totalPrice);
        ArgumentNullException.ThrowIfNull(timeSlot);
        DateTimeGuard.EnsureUtc(utcNow);

        if (status is not (BookingStatus.Pending or BookingStatus.Confirmed))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status), status, "Only pending or confirmed bookings can be cancelled.");
        }

        if (utcNow >= timeSlot.Start)
        {
            return BookingErrors.AlreadyStarted;
        }

        if (status == BookingStatus.Pending)
        {
            return Money.Zero(totalPrice.Currency);
        }

        var notice = timeSlot.Start - utcNow;

        return totalPrice.Multiply(GetRefundRate(notice));
    }

    private static decimal GetRefundRate(TimeSpan notice)
    {
        if (notice >= FullRefundMinimumNotice)
        {
            return FullRefundRate;
        }

        if (notice >= PartialRefundMinimumNotice)
        {
            return PartialRefundRate;
        }

        return NoRefundRate;
    }
}
