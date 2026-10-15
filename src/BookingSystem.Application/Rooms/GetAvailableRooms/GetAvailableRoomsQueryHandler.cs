using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Application.Rooms.GetAvailableRooms;

internal sealed class GetAvailableRoomsQueryHandler(IRoomQueries roomQueries)
    : IQueryHandler<GetAvailableRoomsQuery, IReadOnlyList<RoomDto>>
{
    public async Task<Result<IReadOnlyList<RoomDto>>> Handle(
        GetAvailableRoomsQuery query,
        CancellationToken cancellationToken)
    {
        var slot = TimeSlot.Create(query.StartUtc, query.EndUtc);
        if (slot.IsFailure)
        {
            return Result.Failure<IReadOnlyList<RoomDto>>(slot.Error);
        }

        var rooms = await roomQueries.GetAvailableAsync(slot.Value, query.MinCapacity, cancellationToken);

        return Result.Success(rooms);
    }
}
