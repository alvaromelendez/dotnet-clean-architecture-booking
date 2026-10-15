namespace BookingSystem.Api.Contracts.Bookings;

/// <summary>
/// Reservation request. <paramref name="UserId"/> travels in the body until authentication exists.
/// </summary>
/// <param name="Start">Inclusive start, ISO 8601 with offset (any offset is accepted).</param>
/// <param name="End">Exclusive end, ISO 8601 with offset (any offset is accepted).</param>
public sealed record CreateBookingRequest(
    Guid RoomId,
    Guid UserId,
    DateTimeOffset Start,
    DateTimeOffset End,
    int Attendees);
