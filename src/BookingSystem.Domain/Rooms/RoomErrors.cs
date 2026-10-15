using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Rooms;

public static class RoomErrors
{
    public static readonly Error NameRequired = new(
        "Room.NameRequired",
        "The room name is required.");

    public static readonly Error NameTooLong = new(
        "Room.NameTooLong",
        $"The room name cannot exceed {Room.MaxNameLength} characters.");

    public static readonly Error InvalidCapacity = new(
        "Room.InvalidCapacity",
        "The room capacity must be greater than zero.");

    public static readonly Error AlreadyActive = new(
        "Room.AlreadyActive",
        "The room is already active.");

    public static readonly Error AlreadyInactive = new(
        "Room.AlreadyInactive",
        "The room is already inactive.");
}
