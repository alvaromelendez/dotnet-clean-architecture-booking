using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Bookings.Events;

public sealed record BookingCompletedDomainEvent(
    BookingId BookingId,
    DateTime CompletedAtUtc) : IDomainEvent;
