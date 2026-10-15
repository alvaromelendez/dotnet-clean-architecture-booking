namespace BookingSystem.Api.Contracts.Bookings;

/// <param name="StartUtc">Inclusive start in UTC.</param>
/// <param name="EndUtc">Exclusive end in UTC.</param>
/// <param name="RefundAmount">Present only once the booking is cancelled.</param>
public sealed record BookingResponse(
    Guid Id,
    Guid RoomId,
    Guid UserId,
    DateTime StartUtc,
    DateTime EndUtc,
    int Attendees,
    decimal TotalPrice,
    string Currency,
    BookingStatusContract Status,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? CancelledAtUtc,
    decimal? RefundAmount);
