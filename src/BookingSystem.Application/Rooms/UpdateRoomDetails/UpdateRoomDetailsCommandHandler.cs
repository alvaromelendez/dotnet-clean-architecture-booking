using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Application.Rooms.UpdateRoomDetails;

internal sealed class UpdateRoomDetailsCommandHandler(IRoomRepository roomRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateRoomDetailsCommand>
{
    public async Task<Result> Handle(UpdateRoomDetailsCommand command, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdAsync(RoomId.From(command.RoomId), cancellationToken);
        if (room is null)
        {
            return RoomApplicationErrors.NotFound(command.RoomId);
        }

        var hourlyRate = Money.Create(command.HourlyRate, command.Currency);
        if (hourlyRate.IsFailure)
        {
            return hourlyRate.Error;
        }

        var newName = command.Name.Trim();
        var isRenamed = !string.Equals(room.Name, newName, StringComparison.OrdinalIgnoreCase);
        if (isRenamed && await roomRepository.ExistsByNameAsync(newName, cancellationToken))
        {
            return RoomApplicationErrors.NameNotUnique(newName);
        }

        var result = room.UpdateDetails(command.Name, command.Capacity, hourlyRate.Value);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
