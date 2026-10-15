using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Rooms.ActivateRoom;

public sealed record ActivateRoomCommand(Guid RoomId) : ICommand;
