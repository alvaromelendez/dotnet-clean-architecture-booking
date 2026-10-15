using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Rooms;

public static class RoomErrors
{
    public static readonly Error NameRequired = Error.Validation(
        "Room.NameRequired",
        "The room name is required.");

    public static readonly Error NameTooLong = Error.Validation(
        "Room.NameTooLong",
        $"The room name cannot exceed {Room.MaxNameLength} characters.");

    public static readonly Error InvalidCapacity = Error.Validation(
        "Room.InvalidCapacity",
        "The room capacity must be greater than zero.");

    public static readonly Error AlreadyActive = Error.Conflict(
        "Room.AlreadyActive",
        "The room is already active.");

    public static readonly Error AlreadyInactive = Error.Conflict(
        "Room.AlreadyInactive",
        "The room is already inactive.");
}
