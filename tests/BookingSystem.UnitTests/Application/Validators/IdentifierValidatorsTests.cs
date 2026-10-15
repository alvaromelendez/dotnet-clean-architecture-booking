using BookingSystem.Application.Bookings.CancelBooking;
using BookingSystem.Application.Bookings.CompleteBooking;
using BookingSystem.Application.Bookings.ConfirmBooking;
using BookingSystem.Application.Bookings.GetBookingById;
using BookingSystem.Application.Rooms.ActivateRoom;
using BookingSystem.Application.Rooms.DeactivateRoom;
using BookingSystem.Application.Rooms.GetRoomById;
using FluentValidation.TestHelper;

namespace BookingSystem.UnitTests.Application.Validators;

/// <summary>
/// Requests that only carry an identifier must reject <see cref="Guid.Empty"/>.
/// </summary>
public sealed class IdentifierValidatorsTests
{
    [Fact]
    public void ConfirmBooking_WithEmptyId_HasBookingIdError() =>
        new ConfirmBookingCommandValidator().TestValidate(new ConfirmBookingCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(command => command.BookingId);

    [Fact]
    public void CancelBooking_WithEmptyId_HasBookingIdError() =>
        new CancelBookingCommandValidator().TestValidate(new CancelBookingCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(command => command.BookingId);

    [Fact]
    public void CompleteBooking_WithEmptyId_HasBookingIdError() =>
        new CompleteBookingCommandValidator().TestValidate(new CompleteBookingCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(command => command.BookingId);

    [Fact]
    public void GetBookingById_WithEmptyId_HasBookingIdError() =>
        new GetBookingByIdQueryValidator().TestValidate(new GetBookingByIdQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(query => query.BookingId);

    [Fact]
    public void ActivateRoom_WithEmptyId_HasRoomIdError() =>
        new ActivateRoomCommandValidator().TestValidate(new ActivateRoomCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(command => command.RoomId);

    [Fact]
    public void DeactivateRoom_WithEmptyId_HasRoomIdError() =>
        new DeactivateRoomCommandValidator().TestValidate(new DeactivateRoomCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(command => command.RoomId);

    [Fact]
    public void GetRoomById_WithEmptyId_HasRoomIdError() =>
        new GetRoomByIdQueryValidator().TestValidate(new GetRoomByIdQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(query => query.RoomId);

    [Fact]
    public void ConfirmBooking_WithNonEmptyId_HasNoErrors() =>
        new ConfirmBookingCommandValidator().TestValidate(new ConfirmBookingCommand(Guid.NewGuid()))
            .ShouldNotHaveAnyValidationErrors();
}
