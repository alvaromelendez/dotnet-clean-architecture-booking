using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Domain.Bookings.Events;

public sealed record BookingCancelledDomainEvent(
    BookingId BookingId,
    RoomId RoomId,
    Money RefundAmount,
    DateTime CancelledAtUtc) : IDomainEvent;
