using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Rooms.Events;

public sealed record RoomDeactivatedDomainEvent(RoomId RoomId) : IDomainEvent;
