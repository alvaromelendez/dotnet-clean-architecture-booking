using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Application.Bookings;

/// <summary>
/// Booking errors that depend on persisted state and therefore cannot be detected by the aggregate itself.
/// </summary>
public static class BookingApplicationErrors
{
    public static Error NotFound(Guid bookingId) => Error.NotFound(
        "Booking.NotFound",
        $"The booking with id '{bookingId}' was not found.");
}
