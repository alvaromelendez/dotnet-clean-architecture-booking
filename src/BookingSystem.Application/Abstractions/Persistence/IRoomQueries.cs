using BookingSystem.Application.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Application.Abstractions.Persistence;

/// <summary>
/// Read-side projections of rooms. Implementations return DTOs directly, without loading aggregates.
/// </summary>
public interface IRoomQueries
{
    Task<RoomDto?> GetByIdAsync(Guid roomId, CancellationToken cancellationToken = default);

    /// <param name="isActive">When set, returns only rooms with that active state.</param>
    Task<IReadOnlyList<RoomDto>> ListAsync(bool? isActive, CancellationToken cancellationToken = default);

    /// <summary>
    /// Active rooms with at least <paramref name="minCapacity"/> seats and no pending or
    /// confirmed booking overlapping <paramref name="slot"/>.
    /// </summary>
    Task<IReadOnlyList<RoomDto>> GetAvailableAsync(
        TimeSlot slot,
        int minCapacity,
        CancellationToken cancellationToken = default);
}
