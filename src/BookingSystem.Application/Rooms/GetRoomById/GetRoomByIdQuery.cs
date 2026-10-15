using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Rooms.GetRoomById;

public sealed record GetRoomByIdQuery(Guid RoomId) : IQuery<RoomDto>;
