using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Application.Bookings.GetBookingById;

internal sealed class GetBookingByIdQueryHandler(IBookingQueries bookingQueries)
    : IQueryHandler<GetBookingByIdQuery, BookingDto>
{
    public async Task<Result<BookingDto>> Handle(GetBookingByIdQuery query, CancellationToken cancellationToken)
    {
        var booking = await bookingQueries.GetByIdAsync(query.BookingId, cancellationToken);

        return booking is null
            ? BookingApplicationErrors.NotFound(query.BookingId)
            : booking;
    }
}
