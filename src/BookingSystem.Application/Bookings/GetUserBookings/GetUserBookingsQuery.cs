using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Paging;
using BookingSystem.Domain.Bookings;

namespace BookingSystem.Application.Bookings.GetUserBookings;

/// <param name="Status">When set, returns only bookings in that status.</param>
/// <param name="Page">1-based page number.</param>
public sealed record GetUserBookingsQuery(
    Guid UserId,
    BookingStatus? Status = null,
    int Page = 1,
    int PageSize = GetUserBookingsQuery.DefaultPageSize) : IQuery<PagedResult<BookingDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
