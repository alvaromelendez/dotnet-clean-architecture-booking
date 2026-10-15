using BookingSystem.Application.Abstractions.Clock;
using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Rooms;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Application.Bookings.CreateBooking;

internal sealed class CreateBookingCommandHandler(
    IRoomRepository roomRepository,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    BookingService bookingService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateBookingCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateBookingCommand command, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdAsync(RoomId.From(command.RoomId), cancellationToken);
        if (room is null)
        {
            return RoomApplicationErrors.NotFound(command.RoomId);
        }

        var slot = TimeSlot.Create(command.StartUtc, command.EndUtc);
        if (slot.IsFailure)
        {
            return slot.Error;
        }

        var existingBookings = await bookingRepository.GetActiveByRoomAsync(room.Id, slot.Value, cancellationToken);

        var booking = bookingService.Reserve(
            room,
            UserId.From(command.UserId),
            slot.Value,
            command.Attendees,
            existingBookings,
            dateTimeProvider.UtcNow);

        if (booking.IsFailure)
        {
            return booking.Error;
        }

        await bookingRepository.AddAsync(booking.Value, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return booking.Value.Id.Value;
    }
}
