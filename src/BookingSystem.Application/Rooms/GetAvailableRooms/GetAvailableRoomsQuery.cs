using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Rooms.GetAvailableRooms;

/// <summary>
/// Active rooms that can host <paramref name="MinCapacity"/> attendees and are free for the whole slot.
/// </summary>
public sealed record GetAvailableRoomsQuery(DateTime StartUtc, DateTime EndUtc, int MinCapacity = 1)
    : IQuery<IReadOnlyList<RoomDto>>;
