using BookingSystem.Domain.Rooms;

namespace BookingSystem.Application.Abstractions.Persistence;

/// <summary>
/// Write-side access to <see cref="Room"/> aggregates. Changes are committed by <see cref="IUnitOfWork"/>.
/// </summary>
public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(RoomId roomId, CancellationToken cancellationToken = default);

    Task AddAsync(Room room, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a room with <paramref name="name"/> already exists (case-insensitive).
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}
