using BookingSystem.Application.Abstractions.Persistence;
using BookingSystem.Application.Bookings;
using BookingSystem.Application.Bookings.ConfirmBooking;
using BookingSystem.Domain.Bookings;
using BookingSystem.UnitTests.Application.Fakes;
using NSubstitute;
using Shouldly;
using static BookingSystem.UnitTests.Domain.TestData;

namespace BookingSystem.UnitTests.Application.Bookings;

public sealed class ConfirmBookingCommandHandlerTests
{
    private readonly IBookingRepository _bookingRepository = Substitute.For<IBookingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FakeDateTimeProvider _dateTimeProvider = new();
    private readonly ConfirmBookingCommandHandler _handler;

    public ConfirmBookingCommandHandlerTests()
    {
        _handler = new ConfirmBookingCommandHandler(_bookingRepository, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenBookingIsPending_ConfirmsAndSavesChanges()
    {
        var booking = GivenBooking(BookingStatus.Pending);
        _dateTimeProvider.Advance(TimeSpan.FromMinutes(10));

        var result = await _handler.Handle(new ConfirmBookingCommand(booking.Id.Value), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        booking.Status.ShouldBe(BookingStatus.Confirmed);
        booking.ConfirmedAtUtc.ShouldBe(_dateTimeProvider.UtcNow);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBookingDoesNotExist_ReturnsNotFoundAndDoesNotSave()
    {
        var bookingId = Guid.NewGuid();

        var result = await _handler.Handle(new ConfirmBookingCommand(bookingId), CancellationToken.None);

        result.Error.ShouldBe(BookingApplicationErrors.NotFound(bookingId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public async Task Handle_WhenTransitionIsInvalid_ReturnsNotPendingAndDoesNotSave(BookingStatus status)
    {
        var booking = GivenBooking(status);

        var result = await _handler.Handle(new ConfirmBookingCommand(booking.Id.Value), CancellationToken.None);

        result.Error.ShouldBe(BookingErrors.NotPending);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private Booking GivenBooking(BookingStatus status)
    {
        var booking = new BookingBuilder().InStatus(status).Build();
        _bookingRepository.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        return booking;
    }
}
