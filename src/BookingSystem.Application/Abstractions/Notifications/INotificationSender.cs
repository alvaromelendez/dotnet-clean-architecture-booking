using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Application.Abstractions.Notifications;

/// <summary>
/// Sends booking notifications to users (email, SMS, ...). Implemented in Infrastructure.
/// </summary>
public interface INotificationSender
{
    Task SendBookingConfirmedAsync(BookingId bookingId, CancellationToken cancellationToken = default);

    Task SendBookingCancelledAsync(
        BookingId bookingId,
        Money refundAmount,
        CancellationToken cancellationToken = default);
}
