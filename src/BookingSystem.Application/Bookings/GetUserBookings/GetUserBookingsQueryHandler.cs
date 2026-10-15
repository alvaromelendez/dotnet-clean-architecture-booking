using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Paging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Application.Bookings.GetUserBookings;

internal sealed class GetUserBookingsQueryHandler(IBookingQueries bookingQueries)
    : IQueryHandler<GetUserBookingsQuery, PagedResult<BookingDto>>
{
    public async Task<Result<PagedResult<BookingDto>>> Handle(
        GetUserBookingsQuery query,
        CancellationToken cancellationToken)
    {
        var bookings = await bookingQueries.GetByUserAsync(
            query.UserId,
            query.Status,
            query.Page,
            query.PageSize,
            cancellationToken);

        return bookings;
    }
}
