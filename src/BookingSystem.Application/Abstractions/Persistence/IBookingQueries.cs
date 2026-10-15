using BookingSystem.Application.Abstractions.Paging;
using BookingSystem.Application.Bookings;
using BookingSystem.Domain.Bookings;

namespace BookingSystem.Application.Abstractions.Persistence;

/// <summary>
/// Read-side projections of bookings. Implementations return DTOs directly, without loading aggregates.
/// </summary>
public interface IBookingQueries
{
    Task<BookingDto?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bookings of <paramref name="userId"/>, most recent slot first.
    /// </summary>
    /// <param name="status">When set, returns only bookings in that status.</param>
    /// <param name="page">1-based page number.</param>
    Task<PagedResult<BookingDto>> GetByUserAsync(
        Guid userId,
        BookingStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
