using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Application.Rooms.CreateRoom;

internal sealed class CreateRoomCommandHandler(IRoomRepository roomRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateRoomCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateRoomCommand command, CancellationToken cancellationToken)
    {
        var hourlyRate = Money.Create(command.HourlyRate, command.Currency);
        if (hourlyRate.IsFailure)
        {
            return hourlyRate.Error;
        }

        var room = Room.Create(command.Name, command.Capacity, hourlyRate.Value);
        if (room.IsFailure)
        {
            return room.Error;
        }

        // Checked against the normalized name produced by the aggregate.
        if (await roomRepository.ExistsByNameAsync(room.Value.Name, cancellationToken))
        {
            return RoomApplicationErrors.NameNotUnique(room.Value.Name);
        }

        await roomRepository.AddAsync(room.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return room.Value.Id.Value;
    }
}
