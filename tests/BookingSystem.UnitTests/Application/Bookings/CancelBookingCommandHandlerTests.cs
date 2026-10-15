using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Bookings;
using BookingSystem.Application.Bookings.CancelBooking;
using BookingSystem.Domain.Bookings;
using BookingSystem.UnitTests.Application.Fakes;
using NSubstitute;
using Shouldly;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Application.Bookings;

public sealed class CancelBookingCommandHandlerTests
{
    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeDateTimeProvider _dateTimeProvider = new();
    private readonly CancelBookingCommandHandler _handler;

    public CancelBookingCommandHandlerTests()
    {
        _handler = new CancelBookingCommandHandler(_bookingRepository, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenConfirmedWithFullNotice_CancelsSavesAndReturnsFullRefund()
    {
        var booking = GivenBooking(BookingStatus.Confirmed);

        var result = await _handler.Handle(new CancelBookingCommand(booking.Id.Value), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new CancelBookingResponse(
            booking.Id.Value, booking.TotalPrice.Amount, booking.TotalPrice.Currency));
        booking.Status.ShouldBe(BookingStatus.Cancelled);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConfirmedWithPartialNotice_ReturnsHalfRefund()
    {
        var booking = GivenBooking(BookingStatus.Confirmed);
        _dateTimeProvider.SetUtcNow(booking.TimeSlot.Start.AddHours(-3));

        var result = await _handler.Handle(new CancelBookingCommand(booking.Id.Value), CancellationToken.None);

        result.Value.RefundAmount.ShouldBe(booking.TotalPrice.Amount * CancellationPolicy.PartialRefundRate);
    }

    [Fact]
    public async Task Handle_WhenBookingDoesNotExist_ReturnsNotFoundAndDoesNotSave()
    {
        var bookingId = Guid.NewGuid();

        var result = await _handler.Handle(new CancelBookingCommand(bookingId), CancellationToken.None);

        result.Error.ShouldBe(BookingApplicationErrors.NotFound(bookingId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Theory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public async Task Handle_WhenTransitionIsInvalid_ReturnsNotCancellableAndDoesNotSave(BookingStatus status)
    {
        var booking = GivenBooking(status);

        var result = await _handler.Handle(new CancelBookingCommand(booking.Id.Value), CancellationToken.None);

        result.Error.ShouldBe(BookingErrors.NotCancellable);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenSlotAlreadyStarted_ReturnsAlreadyStartedAndDoesNotSave()
    {
        var booking = GivenBooking(BookingStatus.Confirmed);
        _dateTimeProvider.SetUtcNow(booking.TimeSlot.Start);

        var result = await _handler.Handle(new CancelBookingCommand(booking.Id.Value), CancellationToken.None);

        result.Error.ShouldBe(BookingErrors.AlreadyStarted);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private Booking GivenBooking(BookingStatus status)
    {
        var booking = BookingWithStatus(status);
        _bookingRepository.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        return booking;
    }
}
