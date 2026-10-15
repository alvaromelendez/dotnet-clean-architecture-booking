using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Bookings.ConfirmBooking;

public sealed record ConfirmBookingCommand(Guid BookingId) : ICommand;
