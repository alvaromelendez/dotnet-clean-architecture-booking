using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Bookings.Events;
using BookingSystem.TestUtilities;
using Shouldly;

namespace BookingSystem.UnitTests.Builders;

/// <summary>
/// The builders are shared test infrastructure: if they silently produced the wrong state,
/// every test built on them would verify something other than what it claims.
/// </summary>
public sealed class BuilderTests
{
    [Fact]
    public void TimeSlotBuilder_WithDefaults_StartsThreeDaysAfterTestClockAndLastsTwoHours()
    {
        var slot = new TimeSlotBuilder().Build();

        slot.Start.ShouldBe(TestClock.UtcNow.AddDays(3));
        slot.Duration.ShouldBe(TimeSpan.FromHours(2));
        slot.Start.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void TimeSlotBuilder_WithInvalidDuration_ThrowsNamingTheDomainError()
    {
        var exception = Should.Throw<InvalidOperationException>(
            () => new TimeSlotBuilder().Lasting(TimeSpan.FromMinutes(10)).Build());

        exception.Message.ShouldContain("TimeSlot.TooShort");
    }

    [Fact]
    public void RoomBuilder_WithOptions_BuildsRequestedRoomWithoutPendingEvents()
    {
        var room = new RoomBuilder().Named("Court 1").WithCapacity(4).WithHourlyRate(12m, "EUR").Inactive().Build();

        room.Name.ShouldBe("Court 1");
        room.Capacity.ShouldBe(4);
        room.HourlyRate.Amount.ShouldBe(12m);
        room.HourlyRate.Currency.ShouldBe("EUR");
        room.IsActive.ShouldBeFalse();
        room.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RoomBuilder_WithUniqueName_GeneratesDifferentNames()
    {
        new RoomBuilder().WithUniqueName().Build().Name
            .ShouldNotBe(new RoomBuilder().WithUniqueName().Build().Name);
    }

    [Fact]
    public void BookingBuilder_WithDefaults_BuildsPendingBookingWithoutPendingEvents()
    {
        var booking = new BookingBuilder().Build();

        booking.Status.ShouldBe(BookingStatus.Pending);
        booking.Attendees.ShouldBe(BookingBuilder.DefaultAttendees);
        booking.CreatedAtUtc.ShouldBe(TestClock.UtcNow);
        booking.TimeSlot.ShouldBe(new TimeSlotBuilder().Build());
        booking.DomainEvents.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public void BookingBuilder_InStatus_ReachesThatStatus(BookingStatus status)
    {
        new BookingBuilder().InStatus(status).Build().Status.ShouldBe(status);
    }

    [Fact]
    public void BookingBuilder_InStatus_OverridesPreviousTransitions()
    {
        new BookingBuilder().Completed().InStatus(BookingStatus.Pending).Build().Status.ShouldBe(BookingStatus.Pending);
    }

    [Fact]
    public void BookingBuilder_CancelledWhilePending_HasZeroRefund()
    {
        var booking = new BookingBuilder().Cancelled().Build();

        booking.ConfirmedAtUtc.ShouldBeNull();
        booking.RefundAmount!.Amount.ShouldBe(0m);
    }

    [Fact]
    public void BookingBuilder_ConfirmedThenCancelled_RefundsPerPolicy()
    {
        var booking = new BookingBuilder().Confirmed().Cancelled().Build();

        booking.Status.ShouldBe(BookingStatus.Cancelled);
        booking.RefundAmount.ShouldBe(booking.TotalPrice);
    }

    [Fact]
    public void BookingBuilder_KeepingDomainEvents_KeepsEventsRaisedWhileBuilding()
    {
        var booking = new BookingBuilder().Confirmed().KeepingDomainEvents().Build();

        booking.DomainEvents.Select(domainEvent => domainEvent.GetType())
            .ShouldBe([typeof(BookingCreatedDomainEvent), typeof(BookingConfirmedDomainEvent)]);
    }

    [Fact]
    public void BookingBuilder_WhenDomainRejectsReservation_ThrowsNamingTheDomainError()
    {
        var exception = Should.Throw<InvalidOperationException>(
            () => new BookingBuilder().ForRoom(new RoomBuilder().WithCapacity(2).Build()).WithAttendees(3).Build());

        exception.Message.ShouldContain(BookingErrors.CapacityExceeded.Code);
    }

    [Fact]
    public void BookingBuilder_CancelledAndCompleted_Throws()
    {
        Should.Throw<InvalidOperationException>(() => new BookingBuilder().Completed().Cancelled().Build());
    }
}
