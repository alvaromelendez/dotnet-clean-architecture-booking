using BookingSystem.Application.Abstractions.Validation;
using FluentValidation;

namespace BookingSystem.Application.Rooms.CreateRoom;

internal sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty();
        RuleFor(command => command.Currency).MustBeCurrencyCode();
    }
}
