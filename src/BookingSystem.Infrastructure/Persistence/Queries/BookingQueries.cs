using System.Linq.Expressions;
using BookingSystem.Application.Abstractions.Paging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Bookings;
using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Persistence.Queries;

/// <summary>
/// Read-side booking projections: no tracking and SQL-level projection to <see cref="BookingDto"/>.
/// </summary>
internal sealed class BookingQueries(ApplicationDbContext dbContext) : IBookingQueries
{
    private static readonly Expression<Func<Booking, BookingDto>> ToDto = booking => new BookingDto(
        booking.Id.Value,
        booking.RoomId.Value,
        booking.UserId.Value,
        booking.TimeSlot.Start,
        booking.TimeSlot.End,
        booking.Attendees,
        booking.TotalPrice.Amount,
        booking.TotalPrice.Currency,
        booking.Status,
        booking.CreatedAtUtc,
        booking.ConfirmedAtUtc,
        booking.CancelledAtUtc,
        // The refund columns are NULL until the booking is cancelled.
        (decimal?)booking.RefundAmount!.Amount);

    public Task<BookingDto?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var id = BookingId.From(bookingId);

        return dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<BookingDto>> GetByUserAsync(
        Guid userId,
        BookingStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var id = UserId.From(userId);

        var bookings = dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.UserId == id);

        if (status is not null)
        {
            bookings = bookings.Where(booking => booking.Status == status.Value);
        }

        var totalCount = await bookings.CountAsync(cancellationToken);

        // Id breaks ties between bookings created at the same instant, keeping pages stable.
        var items = await bookings
            .OrderByDescending(booking => booking.CreatedAtUtc)
            .ThenBy(booking => booking.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<BookingDto>(items, page, pageSize, totalCount);
    }
}
