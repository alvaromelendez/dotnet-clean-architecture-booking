using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Domain.Bookings.Events;

public sealed record BookingCreatedDomainEvent(
    BookingId BookingId,
    RoomId RoomId,
    UserId UserId,
    TimeSlot TimeSlot,
    Money TotalPrice) : IDomainEvent;
