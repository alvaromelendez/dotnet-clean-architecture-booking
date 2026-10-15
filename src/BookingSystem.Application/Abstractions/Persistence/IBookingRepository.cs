using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Application.Abstractions.Persistence;

/// <summary>
/// Write-side access to <see cref="Booking"/> aggregates. Changes are committed by <see cref="IUnitOfWork"/>.
/// </summary>
public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(BookingId bookingId, CancellationToken cancellationToken = default);

    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the <see cref="BookingStatus.Pending"/> and <see cref="BookingStatus.Confirmed"/>
    /// bookings of <paramref name="roomId"/> whose time slot overlaps <paramref name="window"/>.
    /// The result feeds the availability check of <see cref="BookingService.Reserve"/>.
    /// </summary>
    Task<IReadOnlyCollection<Booking>> GetActiveByRoomAsync(
        RoomId roomId,
        TimeSlot window,
        CancellationToken cancellationToken = default);
}
