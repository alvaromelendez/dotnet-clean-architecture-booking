using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;
using BookingSystem.TestUtilities;

namespace BookingSystem.IntegrationTests.Infrastructure;

/// <summary>
/// Shortcuts over the shared builders. Names are unique per call because the database is
/// shared across tests and room names must be unique.
/// </summary>
internal static class TestData
{
    public static readonly DateTime UtcNow = TestClock.UtcNow;

    public static Room NewRoom(int capacity = 10, decimal hourlyRate = 20m, string? name = null)
    {
        var builder = new RoomBuilder().WithCapacity(capacity).WithHourlyRate(hourlyRate);

        return (name is null ? builder.WithUniqueName() : builder.Named(name)).Build();
    }

    public static TimeSlot Slot(DateTime start, TimeSpan duration) =>
        new TimeSlotBuilder().Starting(start).Lasting(duration).Build();

    public static TimeSlot SlotStartingIn(TimeSpan startsIn, TimeSpan? duration = null) =>
        new TimeSlotBuilder().StartingIn(startsIn).Lasting(duration ?? TimeSlotBuilder.DefaultDuration).Build();

    /// <summary>
    /// Creates a pending booking without an availability check, simulating requests that raced.
    /// It keeps its <c>BookingCreatedDomainEvent</c>, like a booking that has just been reserved.
    /// </summary>
    public static Booking NewBooking(Room room, TimeSlot slot, UserId? userId = null, DateTime? createdAtUtc = null)
    {
        var builder = new BookingBuilder()
            .ForRoom(room)
            .WithSlot(slot)
            .CreatedAt(createdAtUtc ?? UtcNow)
            .KeepingDomainEvents();

        return (userId is { } user ? builder.ForUser(user) : builder).Build();
    }
}
