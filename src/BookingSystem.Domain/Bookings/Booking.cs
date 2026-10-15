using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Bookings.Events;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Domain.Bookings;

/// <summary>
/// A reservation of a <see cref="Room"/> by a user for a <see cref="Shared.TimeSlot"/>.
/// </summary>
/// <remarks>
/// Bookings can only be created through <see cref="BookingService.Reserve"/>, which validates
/// availability rules that span several aggregates. Status changes follow the state machine
/// documented on <see cref="BookingStatus"/>; invalid transitions return an <see cref="Error"/>.
/// </remarks>
public sealed class Booking : AggregateRoot<BookingId>
{
    private Booking(
        BookingId id,
        RoomId roomId,
        UserId userId,
        TimeSlot timeSlot,
        int attendees,
        Money totalPrice,
        DateTime createdAtUtc)
        : base(id)
    {
        RoomId = roomId;
        UserId = userId;
        TimeSlot = timeSlot;
        Attendees = attendees;
        TotalPrice = totalPrice;
        Status = BookingStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public RoomId RoomId { get; private set; }

    public UserId UserId { get; private set; }

    public TimeSlot TimeSlot { get; private set; }

    public int Attendees { get; private set; }

    public Money TotalPrice { get; private set; }

    public BookingStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ConfirmedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    /// <summary>
    /// Amount returned to the user, set when the booking is cancelled.
    /// </summary>
    public Money? RefundAmount { get; private set; }

    /// <summary>
    /// Whether the booking still holds its time slot, blocking other reservations.
    /// </summary>
    public bool IsActive => Status is BookingStatus.Pending or BookingStatus.Confirmed;

    internal static Booking Create(
        RoomId roomId,
        UserId userId,
        TimeSlot timeSlot,
        int attendees,
        Money totalPrice,
        DateTime utcNow)
    {
        DateTimeGuard.EnsureUtc(utcNow);

        var booking = new Booking(BookingId.New(), roomId, userId, timeSlot, attendees, totalPrice, utcNow);

        booking.Raise(new BookingCreatedDomainEvent(booking.Id, roomId, userId, timeSlot, totalPrice));

        return booking;
    }

    /// <summary>
    /// Pending → Confirmed.
    /// </summary>
    public Result Confirm(DateTime utcNow)
    {
        DateTimeGuard.EnsureUtc(utcNow);

        if (Status != BookingStatus.Pending)
        {
            return BookingErrors.NotPending;
        }

        Status = BookingStatus.Confirmed;
        ConfirmedAtUtc = utcNow;

        Raise(new BookingConfirmedDomainEvent(Id, utcNow));

        return Result.Success();
    }

    /// <summary>
    /// Pending or Confirmed → Cancelled. The refund is computed by <see cref="CancellationPolicy"/>.
    /// </summary>
    public Result Cancel(DateTime utcNow)
    {
        DateTimeGuard.EnsureUtc(utcNow);

        if (!IsActive)
        {
            return BookingErrors.NotCancellable;
        }

        var refund = CancellationPolicy.CalculateRefund(Status, TotalPrice, TimeSlot, utcNow);
        if (refund.IsFailure)
        {
            return refund.Error;
        }

        Status = BookingStatus.Cancelled;
        CancelledAtUtc = utcNow;
        RefundAmount = refund.Value;

        Raise(new BookingCancelledDomainEvent(Id, RoomId, refund.Value, utcNow));

        return Result.Success();
    }

    /// <summary>
    /// Confirmed → Completed, once the time slot has ended.
    /// </summary>
    public Result Complete(DateTime utcNow)
    {
        DateTimeGuard.EnsureUtc(utcNow);

        if (Status != BookingStatus.Confirmed)
        {
            return BookingErrors.NotConfirmed;
        }

        if (utcNow < TimeSlot.End)
        {
            return BookingErrors.NotYetEnded;
        }

        Status = BookingStatus.Completed;

        Raise(new BookingCompletedDomainEvent(Id, utcNow));

        return Result.Success();
    }
}
