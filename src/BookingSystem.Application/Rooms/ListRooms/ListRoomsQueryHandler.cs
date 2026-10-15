using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Application.Rooms.ListRooms;

internal sealed class ListRoomsQueryHandler(IRoomQueries roomQueries)
    : IQueryHandler<ListRoomsQuery, IReadOnlyList<RoomDto>>
{
    public async Task<Result<IReadOnlyList<RoomDto>>> Handle(ListRoomsQuery query, CancellationToken cancellationToken)
    {
        var rooms = await roomQueries.ListAsync(query.IsActive, cancellationToken);

        return Result.Success(rooms);
    }
}
