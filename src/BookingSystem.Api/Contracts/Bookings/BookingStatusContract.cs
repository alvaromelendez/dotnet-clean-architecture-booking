namespace BookingSystem.Api.Contracts.Bookings;

/// <summary>
/// Booking status as exposed over HTTP (serialized as a string).
/// </summary>
/// <remarks>
/// The API owns this type so the domain enum can evolve without breaking clients. Values mirror
/// the domain's <c>BookingStatus</c> one-to-one (names and numbers); a test guards that invariant.
/// </remarks>
public enum BookingStatusContract
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Completed = 4,
}
