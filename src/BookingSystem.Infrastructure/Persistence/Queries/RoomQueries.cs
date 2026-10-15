using System.Linq.Expressions;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Rooms;
using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Persistence.Queries;

/// <summary>
/// Read-side room projections: no tracking and SQL-level projection to <see cref="RoomDto"/>.
/// </summary>
internal sealed class RoomQueries(ApplicationDbContext dbContext) : IRoomQueries
{
    private static readonly Expression<Func<Room, RoomDto>> ToDto = room => new RoomDto(
        room.Id.Value,
        room.Name,
        room.Capacity,
        room.HourlyRate.Amount,
        room.HourlyRate.Currency,
        room.IsActive);

    public Task<RoomDto?> GetByIdAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        var id = RoomId.From(roomId);

        return dbContext.Rooms
            .AsNoTracking()
            .Where(room => room.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoomDto>> ListAsync(bool? isActive, CancellationToken cancellationToken = default)
    {
        var rooms = dbContext.Rooms.AsNoTracking();

        if (isActive is not null)
        {
            rooms = rooms.Where(room => room.IsActive == isActive.Value);
        }

        return await rooms
            .OrderBy(room => room.Name)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoomDto>> GetAvailableAsync(
        TimeSlot slot,
        int minCapacity,
        CancellationToken cancellationToken = default)
    {
        // Translated to NOT EXISTS: conflicting bookings are filtered in the database, never loaded.
        return await dbContext.Rooms
            .AsNoTracking()
            .Where(room => room.IsActive && room.Capacity >= minCapacity)
            .Where(room => !dbContext.Bookings.Any(booking =>
                booking.RoomId == room.Id &&
                (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed) &&
                booking.TimeSlot.Start < slot.End &&
                slot.Start < booking.TimeSlot.End))
            .OrderBy(room => room.Name)
            .Select(ToDto)
            .ToListAsync(cancellationToken);
    }
}
