using BookingSystem.Application.Abstractions.Clock;
using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Bookings;

namespace BookingSystem.Application.Bookings.CancelBooking;

internal sealed class CancelBookingCommandHandler(
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CancelBookingCommand, CancelBookingResponse>
{
    public async Task<Result<CancelBookingResponse>> Handle(
        CancelBookingCommand command,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(BookingId.From(command.BookingId), cancellationToken);
        if (booking is null)
        {
            return BookingApplicationErrors.NotFound(command.BookingId);
        }

        var result = booking.Cancel(dateTimeProvider.UtcNow);
        if (result.IsFailure)
        {
            return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var refund = booking.RefundAmount!;

        return new CancelBookingResponse(booking.Id.Value, refund.Amount, refund.Currency);
    }
}
