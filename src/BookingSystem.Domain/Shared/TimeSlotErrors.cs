using BookingSystem.Domain.Abstractions;

namespace BookingSystem.Domain.Shared;

public static class TimeSlotErrors
{
    public static readonly Error NotUtc = Error.Validation(
        "TimeSlot.NotUtc",
        "The start and end of a time slot must be expressed in UTC.");

    public static readonly Error EndNotAfterStart = Error.Validation(
        "TimeSlot.EndNotAfterStart",
        "The end of a time slot must be after its start.");

    public static readonly Error TooShort = Error.Validation(
        "TimeSlot.TooShort",
        $"A time slot must last at least {TimeSlot.MinimumDuration.TotalMinutes} minutes.");

    public static readonly Error TooLong = Error.Validation(
        "TimeSlot.TooLong",
        $"A time slot cannot last more than {TimeSlot.MaximumDuration.TotalHours} hours.");
}
