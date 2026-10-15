namespace BookingSystem.Application.Bookings.CancelBooking;

public sealed record CancelBookingResponse(Guid BookingId, decimal RefundAmount, string Currency);
