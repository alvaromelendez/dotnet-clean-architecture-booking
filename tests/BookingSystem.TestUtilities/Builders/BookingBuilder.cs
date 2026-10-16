using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;

namespace BookingSystem.TestUtilities.Builders;

/// <summary>
/// Builds valid <see cref="Booking"/>s the only way production code can: <see cref="BookingService.Reserve"/>
/// followed by the public state transitions (<see cref="Booking.Confirm"/>, <see cref="Booking.Cancel"/>,
/// <see cref="Booking.Complete"/>). No reflection is used, so a builder can never produce a state the
/// domain would reject.
/// </summary>
/// <remarks>
/// Defaults: a <see cref="RoomBuilder"/> room, a new user, a <see cref="TimeSlotBuilder"/> slot,
/// 2 attendees, created at <see cref="TestClock.UtcNow"/> and left <see cref="BookingStatus.Pending"/>.
/// Reserve receives no existing bookings, so a builder can also create overlapping bookings to
/// simulate requests that raced past the availability check.
/// Domain events raised while building are cleared unless <see cref="KeepingDomainEvents"/> is called,
/// so the booking looks like one loaded by a repository.
/// </remarks>
public sealed class BookingBuilder
{
    public const int DefaultAttendees = 2;

    private readonly TimeSlotBuilder _slotBuilder = new();
    private Room? _room;
    private UserId? _userId;
    private TimeSlot? _slot;
    private int _attendees = DefaultAttendees;
    private DateTime _createdAtUtc = TestClock.UtcNow;
    private bool _confirm;
    private DateTime? _confirmedAtUtc;
    private bool _cancel;
    private DateTime? _cancelledAtUtc;
    private bool _complete;
    private DateTime? _completedAtUtc;
    private bool _keepDomainEvents;

    public BookingBuilder ForRoom(Room room)
    {
        _room = room;
        return this;
    }

    public BookingBuilder ForUser(UserId userId)
    {
        _userId = userId;
        return this;
    }

    public BookingBuilder WithSlot(TimeSlot slot)
    {
        _slot = slot;
        return this;
    }

    public BookingBuilder Starting(DateTime startUtc)
    {
        _slot = null;
        _slotBuilder.Starting(startUtc);
        return this;
    }

    public BookingBuilder StartingIn(TimeSpan leadTime)
    {
        _slot = null;
        _slotBuilder.StartingIn(leadTime);
        return this;
    }

    public BookingBuilder Lasting(TimeSpan duration)
    {
        _slot = null;
        _slotBuilder.Lasting(duration);
        return this;
    }

    public BookingBuilder WithAttendees(int attendees)
    {
        _attendees = attendees;
        return this;
    }

    public BookingBuilder CreatedAt(DateTime createdAtUtc)
    {
        _createdAtUtc = createdAtUtc;
        return this;
    }

    /// <summary>
    /// Pending → Confirmed, at <paramref name="atUtc"/> (default: the creation time).
    /// </summary>
    public BookingBuilder Confirmed(DateTime? atUtc = null)
    {
        _confirm = true;
        _confirmedAtUtc = atUtc;
        return this;
    }

    /// <summary>
    /// Cancels the booking at <paramref name="atUtc"/> (default: the creation time). Combined with
    /// <see cref="Confirmed"/> it cancels a confirmed booking (refund per policy); alone it cancels
    /// a pending one (zero refund).
    /// </summary>
    public BookingBuilder Cancelled(DateTime? atUtc = null)
    {
        _cancel = true;
        _cancelledAtUtc = atUtc;
        return this;
    }

    /// <summary>
    /// Confirms the booking and completes it at <paramref name="atUtc"/> (default: the end of the slot).
    /// </summary>
    public BookingBuilder Completed(DateTime? atUtc = null)
    {
        _confirm = true;
        _complete = true;
        _completedAtUtc = atUtc;
        return this;
    }

    /// <summary>
    /// Reaches <paramref name="status"/> through the shortest valid path from <see cref="BookingStatus.Pending"/>.
    /// </summary>
    public BookingBuilder InStatus(BookingStatus status)
    {
        _confirm = _cancel = _complete = false;

        return status switch
        {
            BookingStatus.Pending => this,
            BookingStatus.Confirmed => Confirmed(),
            BookingStatus.Cancelled => Cancelled(),
            BookingStatus.Completed => Completed(),
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown booking status."),
        };
    }

    public BookingBuilder KeepingDomainEvents()
    {
        _keepDomainEvents = true;
        return this;
    }

    public Booking Build()
    {
        if (_cancel && _complete)
        {
            throw new InvalidOperationException("A booking cannot be both cancelled and completed.");
        }

        var booking = new BookingService()
            .Reserve(
                _room ?? new RoomBuilder().Build(),
                _userId ?? UserId.New(),
                _slot ?? _slotBuilder.Build(),
                _attendees,
                existingBookingsForRoom: [],
                _createdAtUtc)
            .ValueOrThrow("reserve the booking");

        if (_confirm)
        {
            booking.Confirm(_confirmedAtUtc ?? _createdAtUtc).ThrowIfFailure("confirm the booking");
        }

        if (_cancel)
        {
            booking.Cancel(_cancelledAtUtc ?? _createdAtUtc).ThrowIfFailure("cancel the booking");
        }

        if (_complete)
        {
            booking.Complete(_completedAtUtc ?? booking.TimeSlot.End).ThrowIfFailure("complete the booking");
        }

        if (!_keepDomainEvents)
        {
            booking.ClearDomainEvents();
        }

        return booking;
    }
}
