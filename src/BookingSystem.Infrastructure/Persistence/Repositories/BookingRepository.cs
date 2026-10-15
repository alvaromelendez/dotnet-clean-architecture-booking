using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Persistence.Repositories;

/// <summary>
/// Tracks <see cref="Booking"/> aggregates. Changes are committed by the unit of work, never here.
/// </summary>
internal sealed class BookingRepository(ApplicationDbContext dbContext) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(BookingId bookingId, CancellationToken cancellationToken = default) =>
        dbContext.Bookings.FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);

    public Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        dbContext.Bookings.Add(booking);

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<Booking>> GetActiveByRoomAsync(
        RoomId roomId,
        TimeSlot window,
        CancellationToken cancellationToken = default)
    {
        // Same half-open overlap test as TimeSlot.OverlapsWith: start < other.End && other.Start < end.
        // Loaded without tracking: these bookings are only read by the availability check.
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.RoomId == roomId &&
                (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed) &&
                booking.TimeSlot.Start < window.End &&
                window.Start < booking.TimeSlot.End)
            .ToListAsync(cancellationToken);
    }
}
