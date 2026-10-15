using BookingSystem.Application.Abstractions.Notifications;
using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace BookingSystem.Infrastructure.Notifications;

/// <summary>
/// Stub sender that only logs. A real implementation (email, SMS, push) would resolve the user's
/// contact details and deliver the message; the log entries carry identifiers only, no personal data.
/// </summary>
internal sealed partial class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger)
    : INotificationSender
{
    public Task SendBookingConfirmedAsync(BookingId bookingId, CancellationToken cancellationToken = default)
    {
        LogBookingConfirmed(logger, bookingId.Value);

        return Task.CompletedTask;
    }

    public Task SendBookingCancelledAsync(
        BookingId bookingId,
        Money refundAmount,
        CancellationToken cancellationToken = default)
    {
        LogBookingCancelled(logger, bookingId.Value, refundAmount.Amount, refundAmount.Currency);

        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification sent: booking {BookingId} confirmed")]
    private static partial void LogBookingConfirmed(ILogger logger, Guid bookingId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Notification sent: booking {BookingId} cancelled with refund {RefundAmount} {Currency}")]
    private static partial void LogBookingCancelled(
        ILogger logger, Guid bookingId, decimal refundAmount, string currency);
}
