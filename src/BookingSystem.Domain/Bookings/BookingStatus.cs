namespace BookingSystem.Domain.Bookings;

/// <summary>
/// Lifecycle of a <see cref="Booking"/>.
/// </summary>
/// <remarks>
/// Allowed transitions:
/// <list type="bullet">
///   <item><see cref="Pending"/> → <see cref="Confirmed"/> or <see cref="Cancelled"/>.</item>
///   <item><see cref="Confirmed"/> → <see cref="Cancelled"/> or <see cref="Completed"/>.</item>
///   <item><see cref="Cancelled"/> and <see cref="Completed"/> are terminal.</item>
/// </list>
/// </remarks>
public enum BookingStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Completed = 4,
}
