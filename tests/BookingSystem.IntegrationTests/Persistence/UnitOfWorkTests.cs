using BookingSystem.Application.Abstractions.Messaging;
using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Bookings.Events;
using BookingSystem.Domain.Rooms;
using BookingSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shouldly;

namespace BookingSystem.IntegrationTests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class UnitOfWorkTests(PostgresFixture fixture)
{
    [Fact]
    public async Task SaveChanges_WithOverlappingActiveBookingSavedConcurrently_ThrowsConcurrencyConflict()
    {
        var room = await SaveRoomAsync();
        var slot = TestData.SlotStartingIn(TimeSpan.FromDays(2));
        // Both requests passed the application-level availability check before either committed.
        var first = TestData.NewBooking(room, slot);
        var second = TestData.NewBooking(room, TestData.Slot(slot.Start.AddMinutes(30), TimeSpan.FromHours(1)));
        await SaveNewAsync(first);

        var exception = await Should.ThrowAsync<ConcurrencyConflictException>(() => SaveNewAsync(second));

        exception.InnerException.ShouldBeAssignableTo<DbUpdateException>()!
            .InnerException.ShouldBeOfType<PostgresException>()
            .SqlState.ShouldBe(PostgresErrorCodes.ExclusionViolation);
    }

    [Fact]
    public async Task SaveChanges_WithAdjacentBooking_Succeeds()
    {
        var room = await SaveRoomAsync();
        var slot = TestData.SlotStartingIn(TimeSpan.FromDays(2));
        await SaveNewAsync(TestData.NewBooking(room, slot));

        // [10:00, 12:00) and [12:00, 13:00) do not overlap.
        await SaveNewAsync(TestData.NewBooking(room, TestData.Slot(slot.End, TimeSpan.FromHours(1))));
    }

    [Fact]
    public async Task SaveChanges_WithOverlapOnCancelledBooking_Succeeds()
    {
        var room = await SaveRoomAsync();
        var slot = TestData.SlotStartingIn(TimeSpan.FromDays(2));
        var cancelled = TestData.NewBooking(room, slot);
        cancelled.Cancel(TestData.UtcNow);
        await SaveNewAsync(cancelled);

        await SaveNewAsync(TestData.NewBooking(room, slot));
    }

    [Fact]
    public async Task SaveChanges_WhenBookingWasModifiedConcurrently_ThrowsConcurrencyConflict()
    {
        var booking = TestData.NewBooking(await SaveRoomAsync(), TestData.SlotStartingIn(TimeSpan.FromDays(2)));
        await SaveNewAsync(booking);

        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var firstCopy = await firstContext.Bookings.SingleAsync(b => b.Id == booking.Id);
        var secondCopy = await secondContext.Bookings.SingleAsync(b => b.Id == booking.Id);

        firstCopy.Confirm(TestData.UtcNow);
        await PostgresFixture.CreateUnitOfWork(firstContext).SaveChangesAsync();

        secondCopy.Cancel(TestData.UtcNow);
        var exception = await Should.ThrowAsync<ConcurrencyConflictException>(
            () => PostgresFixture.CreateUnitOfWork(secondContext).SaveChangesAsync());

        exception.InnerException.ShouldBeOfType<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task SaveChanges_WhenRoomWasModifiedConcurrently_ThrowsConcurrencyConflict()
    {
        var room = await SaveRoomAsync();

        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();
        var firstCopy = await firstContext.Rooms.SingleAsync(r => r.Id == room.Id);
        var secondCopy = await secondContext.Rooms.SingleAsync(r => r.Id == room.Id);

        firstCopy.Deactivate();
        await PostgresFixture.CreateUnitOfWork(firstContext).SaveChangesAsync();

        secondCopy.UpdateDetails("Renamed", 3, secondCopy.HourlyRate);
        await Should.ThrowAsync<ConcurrencyConflictException>(
            () => PostgresFixture.CreateUnitOfWork(secondContext).SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_WithRaisedEvents_PublishesWrappedEventsAfterCommitAndClearsThem()
    {
        var booking = TestData.NewBooking(await SaveRoomAsync(), TestData.SlotStartingIn(TimeSpan.FromDays(2)));
        var publisher = new RecordingPublisher();

        await using var dbContext = fixture.CreateDbContext();
        dbContext.Bookings.Add(booking);
        await PostgresFixture.CreateUnitOfWork(dbContext, publisher).SaveChangesAsync();

        var notification = publisher.Published.ShouldHaveSingleItem()
            .ShouldBeOfType<DomainEventNotification<BookingCreatedDomainEvent>>();
        notification.DomainEvent.BookingId.ShouldBe(booking.Id);
        booking.DomainEvents.ShouldBeEmpty();
        (await CountBookingsAsync(booking.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task SaveChanges_WhenCommitFails_DoesNotPublishEvents()
    {
        var room = await SaveRoomAsync();
        var slot = TestData.SlotStartingIn(TimeSpan.FromDays(2));
        await SaveNewAsync(TestData.NewBooking(room, slot));
        var conflicting = TestData.NewBooking(room, slot);
        var publisher = new RecordingPublisher();

        await using var dbContext = fixture.CreateDbContext();
        dbContext.Bookings.Add(conflicting);
        await Should.ThrowAsync<ConcurrencyConflictException>(
            () => PostgresFixture.CreateUnitOfWork(dbContext, publisher).SaveChangesAsync());

        publisher.Published.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveChanges_WhenEventHandlerFailsAfterCommit_KeepsDataAndDoesNotThrow()
    {
        var booking = TestData.NewBooking(await SaveRoomAsync(), TestData.SlotStartingIn(TimeSpan.FromDays(2)));

        await using var dbContext = fixture.CreateDbContext();
        dbContext.Bookings.Add(booking);
        await PostgresFixture.CreateUnitOfWork(dbContext, new RecordingPublisher(throwOnPublish: true)).SaveChangesAsync();

        (await CountBookingsAsync(booking.Id)).ShouldBe(1);
    }

    private async Task<Room> SaveRoomAsync()
    {
        var room = TestData.NewRoom();
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Rooms.Add(room);
        await PostgresFixture.CreateUnitOfWork(dbContext).SaveChangesAsync();

        return room;
    }

    private async Task SaveNewAsync(Booking booking)
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Bookings.Add(booking);
        await PostgresFixture.CreateUnitOfWork(dbContext).SaveChangesAsync();
    }

    private async Task<int> CountBookingsAsync(BookingId bookingId)
    {
        await using var dbContext = fixture.CreateDbContext();

        return await dbContext.Bookings.CountAsync(b => b.Id == bookingId);
    }
}
