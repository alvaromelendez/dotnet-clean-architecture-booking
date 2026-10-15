using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Bookings;

public static class BookingErrors
{
    // Reservation rules (BookingService)

    public static readonly Error RoomInactive = Error.Conflict(
        "Booking.RoomInactive",
        "The room is not active and cannot be booked.");

    public static readonly Error InvalidAttendees = Error.Validation(
        "Booking.InvalidAttendees",
        $"A booking must have at least {BookingService.MinAttendees} attendee.");

    public static readonly Error CapacityExceeded = Error.Validation(
        "Booking.CapacityExceeded",
        "The number of attendees exceeds the room capacity.");

    public static readonly Error StartNotInFuture = Error.Validation(
        "Booking.StartNotInFuture",
        "The time slot must start in the future.");

    public static readonly Error TooFarInAdvance = Error.Validation(
        "Booking.TooFarInAdvance",
        $"Bookings cannot be made more than {BookingService.MaxDaysInAdvance} days in advance.");

    public static readonly Error Overlapping = Error.Conflict(
        "Booking.Overlapping",
        "The time slot overlaps with an existing booking for this room.");

    // State transitions (Booking)

    public static readonly Error NotPending = Error.Conflict(
        "Booking.NotPending",
        "Only pending bookings can be confirmed.");

    public static readonly Error NotCancellable = Error.Conflict(
        "Booking.NotCancellable",
        "Only pending or confirmed bookings can be cancelled.");

    public static readonly Error AlreadyStarted = Error.Conflict(
        "Booking.AlreadyStarted",
        "A booking cannot be cancelled once its time slot has started.");

    public static readonly Error NotConfirmed = Error.Conflict(
        "Booking.NotConfirmed",
        "Only confirmed bookings can be completed.");

    public static readonly Error NotYetEnded = Error.Conflict(
        "Booking.NotYetEnded",
        "A booking cannot be completed before its time slot ends.");
}
