using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Bookings.CancelBooking;

/// <summary>
/// Cancels a booking and returns the refund computed by the cancellation policy.
/// </summary>
public sealed record CancelBookingCommand(Guid BookingId) : ICommand<CancelBookingResponse>;
