using BookingSystem.Application.Abstractions.Messaging;

namespace BookingSystem.Application.Bookings.GetBookingById;

public sealed record GetBookingByIdQuery(Guid BookingId) : IQuery<BookingDto>;
