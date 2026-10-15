using BookingSystem.Application.Abstractions.Clock;
using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Bookings;

namespace BookingSystem.Application.Bookings.CompleteBooking;

internal sealed class CompleteBookingCommandHandler(
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CompleteBookingCommand>
{
    public async Task<Result> Handle(CompleteBookingCommand command, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(BookingId.From(command.BookingId), cancellationToken);
        if (booking is null)
        {
            return BookingApplicationErrors.NotFound(command.BookingId);
        }

        var result = booking.Complete(dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
