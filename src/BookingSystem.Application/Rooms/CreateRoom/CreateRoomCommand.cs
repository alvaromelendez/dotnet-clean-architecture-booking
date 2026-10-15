using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Rooms.CreateRoom;

/// <summary>
/// Creates a room and returns its id.
/// </summary>
public sealed record CreateRoomCommand(string Name, int Capacity, decimal HourlyRate, string Currency)
    : ICommand<Guid>;
