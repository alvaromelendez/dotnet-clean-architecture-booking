using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Bookings.Events;

public sealed record BookingConfirmedDomainEvent(
    BookingId BookingId,
    DateTime ConfirmedAtUtc) : IDomainEvent;
