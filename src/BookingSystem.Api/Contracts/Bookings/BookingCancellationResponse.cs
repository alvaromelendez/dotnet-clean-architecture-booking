namespace BookingSystem.Api.Contracts.Bookings;

/// <summary>
/// Outcome of a cancellation: the refund computed by the cancellation policy.
/// </summary>
public sealed record BookingCancellationResponse(Guid BookingId, decimal RefundAmount, string Currency);
