using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Rooms;

namespace BookingSystem.Application.Rooms.ActivateRoom;

internal sealed class ActivateRoomCommandHandler(IRoomRepository roomRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<ActivateRoomCommand>
{
    public async Task<Result> Handle(ActivateRoomCommand command, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdAsync(RoomId.From(command.RoomId), cancellationToken);
        if (room is null)
        {
            return RoomApplicationErrors.NotFound(command.RoomId);
        }

        var result = room.Activate();
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
