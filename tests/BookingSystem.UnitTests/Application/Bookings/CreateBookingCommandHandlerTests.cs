using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Bookings.CreateBooking;
using BookingSystem.Application.Rooms;
using BookingSystem.Domain.Abstractions;
using BookingSystem.Domain.Bookings;
using BookingSystem.Domain.Rooms;
using BookingSystem.Domain.Shared;
using BookingSystem.UnitTests.Application.Fakes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Application.Bookings;

public sealed class CreateBookingCommandHandlerTests
{
    private readonly IRoomRepository _roomRepository = Substitute.For<IRoomRepository>();
    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeDateTimeProvider _dateTimeProvider = new();
    private readonly CreateBookingCommandHandler _handler;

    private readonly Room _room = ActiveRoom(capacity: 10, hourlyRate: 20m);
    private readonly TimeSlot _slot = SlotStartingIn(TimeSpan.FromDays(2), TimeSpan.FromHours(2));

    public CreateBookingCommandHandlerTests()
    {
        _roomRepository.GetByIdAsync(_room.Id, Arg.Any<CancellationToken>()).Returns(_room);
        _bookingRepository
            .GetActiveByRoomAsync(Arg.Any<RoomId>(), Arg.Any<TimeSlot>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Booking>());

        _handler = new CreateBookingCommandHandler(
            _roomRepository, _bookingRepository, _unitOfWork, new BookingService(), _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WithValidCommand_AddsPendingBookingAndSavesChangesOnce()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _bookingRepository.Received(1).AddAsync(
            Arg.Is<Booking>(booking =>
                booking.Id.Value == result.Value &&
                booking.RoomId == _room.Id &&
                booking.Status == BookingStatus.Pending &&
                booking.TotalPrice == Usd(40m) &&
                booking.CreatedAtUtc == _dateTimeProvider.UtcNow),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_LoadsActiveBookingsForRequestedRoomAndSlot()
    {
        await _handler.Handle(Command(), CancellationToken.None);

        await _bookingRepository.Received(1).GetActiveByRoomAsync(_room.Id, _slot, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRoomDoesNotExist_ReturnsNotFoundAndDoesNotPersist()
    {
        var missingRoomId = Guid.NewGuid();

        var result = await _handler.Handle(Command(roomId: missingRoomId), CancellationToken.None);

        result.Error.ShouldBe(RoomApplicationErrors.NotFound(missingRoomId));
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        await AssertNothingPersisted();
    }

    [Fact]
    public async Task Handle_WhenRoomIsInactive_ReturnsRoomInactiveAndDoesNotPersist()
    {
        _room.Deactivate();

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Error.ShouldBe(BookingErrors.RoomInactive);
        await AssertNothingPersisted();
    }

    [Fact]
    public async Task Handle_WhenSlotOverlapsExistingBooking_ReturnsOverlappingAndDoesNotPersist()
    {
        var existing = PendingBooking(_room, _slot);
        _bookingRepository
            .GetActiveByRoomAsync(_room.Id, _slot, Arg.Any<CancellationToken>())
            .Returns([existing]);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Error.ShouldBe(BookingErrors.Overlapping);
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        await AssertNothingPersisted();
    }

    [Fact]
    public async Task Handle_WhenSlotIsInvalid_PropagatesTimeSlotErrorAndDoesNotPersist()
    {
        var command = Command() with { EndUtc = _slot.Start.AddHours(-1) };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Error.ShouldBe(TimeSlotErrors.EndNotAfterStart);
        await _bookingRepository.DidNotReceiveWithAnyArgs().GetActiveByRoomAsync(default, default!, default);
        await AssertNothingPersisted();
    }

    [Fact]
    public async Task Handle_WhenSlotStartsBeforeClockNow_ReturnsStartNotInFuture()
    {
        _dateTimeProvider.SetUtcNow(_slot.Start.AddMinutes(1));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Error.ShouldBe(BookingErrors.StartNotInFuture);
        await AssertNothingPersisted();
    }

    [Fact]
    public async Task Handle_WhenSaveDetectsConcurrentBooking_ReturnsSlotNoLongerAvailableConflict()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyConflictException("Exclusion constraint violated."));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.Error.ShouldBe(BookingErrors.SlotNoLongerAvailable);
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    private CreateBookingCommand Command(Guid? roomId = null) =>
        new(roomId ?? _room.Id.Value, Guid.NewGuid(), _slot.Start, _slot.End, Attendees: 4);

    private async Task AssertNothingPersisted()
    {
        await _bookingRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
