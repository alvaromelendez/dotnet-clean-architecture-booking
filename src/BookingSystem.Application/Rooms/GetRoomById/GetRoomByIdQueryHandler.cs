using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Application.Rooms.GetRoomById;

internal sealed class GetRoomByIdQueryHandler(IRoomQueries roomQueries)
    : IQueryHandler<GetRoomByIdQuery, RoomDto>
{
    public async Task<Result<RoomDto>> Handle(GetRoomByIdQuery query, CancellationToken cancellationToken)
    {
        var room = await roomQueries.GetByIdAsync(query.RoomId, cancellationToken);

        return room is null
            ? RoomApplicationErrors.NotFound(query.RoomId)
            : room;
    }
}
