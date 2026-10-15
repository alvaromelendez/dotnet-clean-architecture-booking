using BookingSystem.Application.Abstractions.Validation;
using FluentValidation;

namespace BookingSystem.Application.Rooms.UpdateRoomDetails;

internal sealed class UpdateRoomDetailsCommandValidator : AbstractValidator<UpdateRoomDetailsCommand>
{
    public UpdateRoomDetailsCommandValidator()
    {
        RuleFor(command => command.RoomId).NotEmpty();
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Currency).MustBeCurrencyCode();
    }
}
