using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Rooms.UpdateRoomDetails;

public sealed record UpdateRoomDetailsCommand(
    Guid RoomId,
    string Name,
    int Capacity,
    decimal HourlyRate,
    string Currency) : ICommand;
