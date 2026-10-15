using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Rooms.ListRooms;

/// <param name="IsActive">When set, returns only rooms with that active state.</param>
public sealed record ListRoomsQuery(bool? IsActive = null) : IQuery<IReadOnlyList<RoomDto>>;
