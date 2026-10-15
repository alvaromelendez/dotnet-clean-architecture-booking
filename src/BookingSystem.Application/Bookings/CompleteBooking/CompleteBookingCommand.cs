using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Bookings.CompleteBooking;

public sealed record CompleteBookingCommand(Guid BookingId) : ICommand;
