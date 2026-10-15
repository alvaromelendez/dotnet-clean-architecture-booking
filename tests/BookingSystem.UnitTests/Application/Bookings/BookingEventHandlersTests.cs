using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Notifications;
using BookingSystem.Application.Bookings.EventHandlers;
using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Bookings.Events;
using BookingSystem.Domain.Rooms;
using NSubstitute;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Application.Bookings;

public sealed class BookingEventHandlersTests
{
    private readonly INotificationSender _notificationSender = Substitute.For<INotificationSender>();

    [Fact]
    public async Task BookingConfirmedHandler_WhenEventIsPublished_SendsConfirmationNotification()
    {
        var bookingId = BookingId.New();
        var handler = new BookingConfirmedDomainEventHandler(_notificationSender);

        await handler.Handle(
            new DomainEventNotification<BookingConfirmedDomainEvent>(new BookingConfirmedDomainEvent(bookingId, UtcNow)),
            CancellationToken.None);

        await _notificationSender.Received(1).SendBookingConfirmedAsync(bookingId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BookingCancelledHandler_WhenEventIsPublished_SendsCancellationNotificationWithRefund()
    {
        var bookingId = BookingId.New();
        var refund = Usd(25m);
        var handler = new BookingCancelledDomainEventHandler(_notificationSender);

        await handler.Handle(
            new DomainEventNotification<BookingCancelledDomainEvent>(
                new BookingCancelledDomainEvent(bookingId, RoomId.New(), refund, UtcNow)),
            CancellationToken.None);

        await _notificationSender.Received(1).SendBookingCancelledAsync(bookingId, refund, Arg.Any<CancellationToken>());
    }
}
