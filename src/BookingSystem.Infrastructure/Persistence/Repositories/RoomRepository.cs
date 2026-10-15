using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Rooms;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Persistence.Repositories;

/// <summary>
/// Tracks <see cref="Room"/> aggregates. Changes are committed by the unit of work, never here.
/// </summary>
internal sealed class RoomRepository(ApplicationDbContext dbContext) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(RoomId roomId, CancellationToken cancellationToken = default) =>
        dbContext.Rooms.FirstOrDefaultAsync(room => room.Id == roomId, cancellationToken);

    public Task AddAsync(Room room, CancellationToken cancellationToken = default)
    {
        dbContext.Rooms.Add(room);

        return Task.CompletedTask;
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        // Compares lower(name) so the lookup matches, and can use, the unique index on lower(name).
        var normalizedName = name.Trim().ToLowerInvariant();

        return dbContext.Rooms.AnyAsync(room => room.Name.ToLower() == normalizedName, cancellationToken);
    }
}
