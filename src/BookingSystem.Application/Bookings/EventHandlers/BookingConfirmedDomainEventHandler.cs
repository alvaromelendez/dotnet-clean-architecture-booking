using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Notifications;
using BookingSystem.Domain.Bookings.Events;

namespace BookingSystem.Application.Bookings.EventHandlers;

internal sealed class BookingConfirmedDomainEventHandler(INotificationSender notificationSender)
    : IDomainEventHandler<BookingConfirmedDomainEvent>
{
    public Task Handle(
        DomainEventNotification<BookingConfirmedDomainEvent> notification,
        CancellationToken cancellationToken) =>
        notificationSender.SendBookingConfirmedAsync(notification.DomainEvent.BookingId, cancellationToken);
}
