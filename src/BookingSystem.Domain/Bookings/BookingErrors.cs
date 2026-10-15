using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Bookings;

public static class BookingErrors
{
    public static readonly Error NotPending = new(
        "Booking.NotPending",
        "Only pending bookings can be confirmed.");

    public static readonly Error NotCancellable = new(
        "Booking.NotCancellable",
        "Only pending or confirmed bookings can be cancelled.");

    public static readonly Error AlreadyStarted = new(
        "Booking.AlreadyStarted",
        "A booking cannot be cancelled once its time slot has started.");

    public static readonly Error NotConfirmed = new(
        "Booking.NotConfirmed",
        "Only confirmed bookings can be completed.");

    public static readonly Error NotYetEnded = new(
        "Booking.NotYetEnded",
        "A booking cannot be completed before its time slot ends.");
}
