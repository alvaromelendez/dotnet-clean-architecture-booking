using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Application.Rooms;

/// <summary>
/// Room errors that depend on persisted state and therefore cannot be detected by the aggregate itself.
/// </summary>
public static class RoomApplicationErrors
{
    public static Error NotFound(Guid roomId) => Error.NotFound(
        "Room.NotFound",
        $"The room with id '{roomId}' was not found.");

    public static Error NameNotUnique(string name) => Error.Conflict(
        "Room.NameNotUnique",
        $"A room named '{name}' already exists.");
}
