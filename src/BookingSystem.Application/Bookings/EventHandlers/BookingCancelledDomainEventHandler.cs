using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Notifications;
using BookingSystem.Domain.Bookings.Events;

namespace BookingSystem.Application.Bookings.EventHandlers;

internal sealed class BookingCancelledDomainEventHandler(INotificationSender notificationSender)
    : IDomainEventHandler<BookingCancelledDomainEvent>
{
    public Task Handle(
        DomainEventNotification<BookingCancelledDomainEvent> notification,
        CancellationToken cancellationToken) =>
        notificationSender.SendBookingCancelledAsync(
            notification.DomainEvent.BookingId,
            notification.DomainEvent.RefundAmount,
            cancellationToken);
}
