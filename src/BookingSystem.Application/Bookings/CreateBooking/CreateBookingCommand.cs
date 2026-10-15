using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Bookings.CreateBooking;

/// <summary>
/// Reserves a room for a time slot and returns the id of the new pending booking.
/// </summary>
public sealed record CreateBookingCommand(
    Guid RoomId,
    Guid UserId,
    DateTime StartUtc,
    DateTime EndUtc,
    int Attendees) : ICommand<Guid>;
