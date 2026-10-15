using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Rooms.Events;
using BookingSystem.Domain.Shared;

namespace BookingSystem.Domain.Rooms;

/// <summary>
/// A bookable space (meeting room, sports court, medical office, ...).
/// </summary>
/// <remarks>
/// <see cref="Activate"/> and <see cref="Deactivate"/> are not idempotent: requesting the state
/// the room is already in returns an error, mirroring how invalid booking transitions are reported
/// and guaranteeing that <see cref="RoomDeactivatedDomainEvent"/> is raised only on a real change.
/// </remarks>
public sealed class Room : AggregateRoot<RoomId>
{
    public const int MaxNameLength = 100;

    private Room(RoomId id, string name, int capacity, Money hourlyRate)
        : base(id)
    {
        Name = name;
        Capacity = capacity;
        HourlyRate = hourlyRate;
        IsActive = true;
    }

    public string Name { get; private set; }

    /// <summary>
    /// Maximum number of attendees the room can host.
    /// </summary>
    public int Capacity { get; private set; }

    public Money HourlyRate { get; private set; }

    /// <summary>
    /// Only active rooms accept new bookings.
    /// </summary>
    public bool IsActive { get; private set; }

    public static Result<Room> Create(string name, int capacity, Money hourlyRate)
    {
        ArgumentNullException.ThrowIfNull(hourlyRate);

        var validation = ValidateDetails(name, capacity);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        return new Room(RoomId.New(), name.Trim(), capacity, hourlyRate);
    }

    public Result UpdateDetails(string name, int capacity, Money hourlyRate)
    {
        ArgumentNullException.ThrowIfNull(hourlyRate);

        var validation = ValidateDetails(name, capacity);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        Capacity = capacity;
        HourlyRate = hourlyRate;

        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
        {
            return RoomErrors.AlreadyActive;
        }

        IsActive = true;

        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
        {
            return RoomErrors.AlreadyInactive;
        }

        IsActive = false;
        Raise(new RoomDeactivatedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Price of booking this room for <paramref name="slot"/>: the hourly rate
    /// prorated to the exact slot duration, rounded to two decimal places.
    /// </summary>
    public Money CalculatePrice(TimeSlot slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var hours = (decimal)slot.Duration.Ticks / TimeSpan.TicksPerHour;

        return HourlyRate.Multiply(hours);
    }

    private static Result ValidateDetails(string name, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return RoomErrors.NameRequired;
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return RoomErrors.NameTooLong;
        }

        if (capacity <= 0)
        {
            return RoomErrors.InvalidCapacity;
        }

        return Result.Success();
    }
}
