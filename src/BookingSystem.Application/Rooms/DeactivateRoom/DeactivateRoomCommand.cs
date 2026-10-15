using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Rooms.DeactivateRoom;

public sealed record DeactivateRoomCommand(Guid RoomId) : ICommand;
