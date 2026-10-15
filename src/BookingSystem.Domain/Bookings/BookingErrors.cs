using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Bookings;

public static class BookingErrors
{
    // Reservation rules (BookingService)

    public static readonly Error RoomInactive = new(
        "Booking.RoomInactive",
        "The room is not active and cannot be booked.");

    public static readonly Error InvalidAttendees = new(
        "Booking.InvalidAttendees",
        $"A booking must have at least {BookingService.MinAttendees} attendee.");

    public static readonly Error CapacityExceeded = new(
        "Booking.CapacityExceeded",
        "The number of attendees exceeds the room capacity.");

    public static readonly Error StartNotInFuture = new(
        "Booking.StartNotInFuture",
        "The time slot must start in the future.");

    public static readonly Error TooFarInAdvance = new(
        "Booking.TooFarInAdvance",
        $"Bookings cannot be made more than {BookingService.MaxDaysInAdvance} days in advance.");

    public static readonly Error Overlapping = new(
        "Booking.Overlapping",
        "The time slot overlaps with an existing booking for this room.");

    // State transitions (Booking)

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
