using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Bookings;
using BookingSystem.Application.Bookings.CompleteBooking;
using BookingSystem.Domain.Bookings;
using BookingSystem.UnitTests.Application.Fakes;
using NSubstitute;
using Shouldly;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Application.Bookings;

public sealed class CompleteBookingCommandHandlerTests
{
    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeDateTimeProvider _dateTimeProvider = new();
    private readonly CompleteBookingCommandHandler _handler;

    public CompleteBookingCommandHandlerTests()
    {
        _handler = new CompleteBookingCommandHandler(_bookingRepository, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenConfirmedAndSlotEnded_CompletesAndSavesChanges()
    {
        var booking = GivenBooking(BookingStatus.Confirmed);
        _dateTimeProvider.SetUtcNow(booking.TimeSlot.End);

        var result = await _handler.Handle(new CompleteBookingCommand(booking.Id.Value), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        booking.Status.ShouldBe(BookingStatus.Completed);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSlotHasNotEnded_ReturnsNotYetEndedAndDoesNotSave()
    {
        var booking = GivenBooking(BookingStatus.Confirmed);

        var result = await _handler.Handle(new CompleteBookingCommand(booking.Id.Value), CancellationToken.None);

        result.Error.ShouldBe(BookingErrors.NotYetEnded);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenBookingDoesNotExist_ReturnsNotFoundAndDoesNotSave()
    {
        var bookingId = Guid.NewGuid();

        var result = await _handler.Handle(new CompleteBookingCommand(bookingId), CancellationToken.None);

        result.Error.ShouldBe(BookingApplicationErrors.NotFound(bookingId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Theory]
    [InlineData(BookingStatus.Pending)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public async Task Handle_WhenTransitionIsInvalid_ReturnsNotConfirmedAndDoesNotSave(BookingStatus status)
    {
        var booking = GivenBooking(status);
        _dateTimeProvider.SetUtcNow(booking.TimeSlot.End);

        var result = await _handler.Handle(new CompleteBookingCommand(booking.Id.Value), CancellationToken.None);

        result.Error.ShouldBe(BookingErrors.NotConfirmed);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private Booking GivenBooking(BookingStatus status)
    {
        var booking = new BookingBuilder().InStatus(status).Build();
        _bookingRepository.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        return booking;
    }
}
