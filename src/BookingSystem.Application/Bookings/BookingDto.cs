using BookingSystem.Domain.Bookings;

namespace BookingSystem.Application.Bookings;

public sealed record BookingDto(
    Guid Id,
    Guid RoomId,
    Guid UserId,
    DateTime StartUtc,
    DateTime EndUtc,
    int Attendees,
    decimal TotalPrice,
    string Currency,
    BookingStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? CancelledAtUtc,
    decimal? RefundAmount);
